handlers.DrawBullet = function (args, context) {

    var drawCount = args.drawCount || 0;
    if (drawCount <= 0)
        return { error: "Invalid drawCount" };

    try {

        // 1️⃣ 티켓 확인 + 차감
        var inv = server.GetUserInventory({ PlayFabId: currentPlayerId });
        var ticket = inv.VirtualCurrency["TK"] || 0;

        if (ticket < drawCount)
            return { error: "Not enough tickets" };

        server.SubtractUserVirtualCurrency({
            PlayFabId: currentPlayerId,
            VirtualCurrency: "TK",
            Amount: drawCount
        });

        // 2️⃣ Title 데이터 한번에 로드
        var titleData = server.GetTitleInternalData({
            Keys: ["ForgeProbabilityTable", "LevelXPTable", "DrawXPTable"]
        });

        var probabilityTable = JSON.parse(titleData.Data["ForgeProbabilityTable"]);
        var levelTable = JSON.parse(titleData.Data["LevelXPTable"]);
        var drawXPTable = JSON.parse(titleData.Data["DrawXPTable"]);

        // 3️⃣ UserData 한번에 로드
        var userData = server.GetUserData({
            PlayFabId: currentPlayerId,
            Keys: ["BulletInventory", "DrawLevelData"]
        });

        // 🔹 인벤토리 로드
        var bulletInventory = { bullets: [] };

        if (userData.Data["BulletInventory"])
            bulletInventory = JSON.parse(userData.Data["BulletInventory"].Value);

        var bullets = bulletInventory.bullets;

        // 🔹 Map 변환 (O(1) 접근)
        var bulletMap = {};
        for (var i = 0; i < bullets.length; i++)
            bulletMap[bullets[i].bulletId] = bullets[i];

        // 🔹 drawLevel 로드
        var drawData = { drawLevel: 1, drawExp: 0 };

        if (userData.Data["DrawLevelData"])
            drawData = JSON.parse(userData.Data["DrawLevelData"].Value);

        var probabilities = probabilityTable[drawData.drawLevel - 1];

        var resultMap = {};

        // =========================
        // 4️⃣ 뽑기 실행
        // =========================
        for (var d = 0; d < drawCount; d++) {

            var tier = RollTier(probabilities);
            if (tier <= 0) continue;

            var bulletId = 1000 + (tier - 1) * 4 + Math.floor(Math.random() * 4);

            if (!resultMap[bulletId])
                resultMap[bulletId] = { bulletId: bulletId, gained: 1 };
            else
                resultMap[bulletId].gained++;

            ApplyBulletGainFast(bulletId, bulletMap, levelTable);
        }

        // =========================
        // 5️⃣ DrawLevel 업데이트 (API 없이 내부 처리)
        // =========================
        drawData.drawExp += drawCount;

        while (drawData.drawLevel <= drawXPTable.length) {

            var required = drawXPTable[drawData.drawLevel - 1];

            if (drawData.drawExp >= required) {
                drawData.drawExp -= required;
                drawData.drawLevel++;
            } else break;
        }

        // =========================
        // 6️⃣ Map → Array 복원
        // =========================
        bulletInventory.bullets = [];
        for (var key in bulletMap)
            bulletInventory.bullets.push(bulletMap[key]);

        // =========================
        // 7️⃣ 저장 (2번만 호출)
        // =========================
        server.UpdateUserData({
            PlayFabId: currentPlayerId,
            Data: {
                "BulletInventory": JSON.stringify(bulletInventory),
                "DrawLevelData": JSON.stringify(drawData)
            }
        });

        // =========================
        // 8️⃣ 반환 데이터 생성
        // =========================
        var resultObjects = [];

        for (var id in resultMap) {

            var b = bulletMap[id];

            resultObjects.push({
                bulletId: parseInt(id),
                gained: resultMap[id].gained,
                finalCount: b.count,
                finalLevel: b.level
            });
        }

        return {
            success: true,
            drawLevel: drawData.drawLevel,
            results: resultObjects
        };

    } catch (e) {

        log.error("DrawBullet Error: " + JSON.stringify(e));
        return { error: "Server error", detail: e };
    }
};


// =========================
// 🔥 초고속 레벨업 처리
// =========================
function ApplyBulletGainFast(bulletId, bulletMap, levelTable) {

    var bullet = bulletMap[bulletId];

    if (!bullet) {
        bullet = {
            bulletId: bulletId,
            level: 1,
            count: 0,
            stats: []
        };
        bulletMap[bulletId] = bullet;
    }

    bullet.count += 1;

    while (bullet.level <= levelTable.length) {

        var requiredXP = levelTable[bullet.level - 1];

        if (bullet.count >= requiredXP) {
            bullet.count -= requiredXP;
            bullet.level++;
        } else break;
    }
}

function RollTier(probabilities) {

    var roll = Math.random() * 100;
    var cumulative = 0;

    for (var i = 0; i < probabilities.length; i++) {
        cumulative += probabilities[i];
        if (roll <= cumulative)
            return i + 1;
    }

    return 0;
}