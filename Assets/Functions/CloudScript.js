// Auto-generated CloudScript 
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
            drawExp: drawData.drawExp,
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
}handlers.UsePlayerGold = function (args, context) {
    var pendingAmount = args.pendingAmount || 0;
    var useAmount = args.useAmount || 0;

    if (useAmount <= 0) {
        return { error: "Invalid useAmount" };
    }

    try {
        var result;

        // 1. pendingAmount 먼저 반영 (항상 양수라고 가정)
        if (pendingAmount > 0) {
            result = server.AddUserVirtualCurrency({
                PlayFabId: currentPlayerId,
                VirtualCurrency: "GD",
                Amount: pendingAmount
            });
        } else {
            // pending이 없으면 현재 잔액 조회
            var inv = server.GetUserInventory({ PlayFabId: currentPlayerId });
            result = { Balance: inv.VirtualCurrency["GD"] || 0 };
        }

        var currentBalance = result.Balance;

        // 2. 사용 가능 여부 확인
        if (currentBalance < useAmount) {
            return { error: "Not enough gold" };
        }

        // 3. 골드 차감
        result = server.SubtractUserVirtualCurrency({
            PlayFabId: currentPlayerId,
            VirtualCurrency: "GD",
            Amount: useAmount
        });

        // 4. 결과 반환
        return {
            success: true,
            newGold: result.Balance,
            addedPending: pendingAmount,
            used: useAmount
        };

    } catch (e) {
        return { error: "Server error during gold update", detail: e };
    }
};

handlers.SyncPendingGold = function (args, context) {
    var pending = args.pendingGold || 0;

    var result = server.AddUserVirtualCurrency({
        PlayFabId: currentPlayerId,
        VirtualCurrency: "GD",
        Amount: pending
    });

    return {
        success: true,
        newGold: result.Balance, // 동기화 후 최종 잔액
        added: pending
    };
};

handlers.SyncPendingTicket = function (args, context) {

    var pending = args.pendingTicket || 0;

    var MAX_ALLOWED = 200;

    // 1️⃣ 음수 방지
    if (pending <= 0) {
        return { error: "Invalid pending amount" };
    }

    // 2️⃣ 과도한 수치 차단 (치트 의심)
    if (pending > MAX_ALLOWED) {

        log.error("Cheat suspected - pendingTicket too high: " + pending);

        // 선택: 치트 플래그 저장
        server.UpdateUserInternalData({
            PlayFabId: currentPlayerId,
            Data: {
                "CheatFlag": "PendingTicketOverflow_" + pending
            }
        });

        return { error: "Abnormal ticket amount detected" };
    }

    // 3️⃣ 정상 지급
    var result = server.AddUserVirtualCurrency({
        PlayFabId: currentPlayerId,
        VirtualCurrency: "TK",
        Amount: pending
    });

    return {
        success: true,
        newTicket: result.Balance,
        added: pending
    };
};// 단일 탄환 스탯 갱신 (포지에서 ApplyNewStats 시 호출)
handlers.UpdateBulletStats = function (args, context) {

    try {
        if (!args.bulletJson)
            return { error: "Missing bulletJson" };

        var incoming = JSON.parse(args.bulletJson);

        if (incoming.bulletId === undefined || incoming.bulletId === null)
            return { error: "Invalid bulletId" };

        // 인벤토리 로드
        var userData = server.GetUserData({
            PlayFabId: currentPlayerId,
            Keys: ["BulletInventory"]
        });

        var bulletInventory = { bullets: [] };

        if (userData.Data["BulletInventory"])
            bulletInventory = JSON.parse(userData.Data["BulletInventory"].Value);

        var bullets = bulletInventory.bullets;

        // 해당 탄환 찾기
        var found = null;
        for (var i = 0; i < bullets.length; i++) {
            if (bullets[i].bulletId === incoming.bulletId) {
                found = bullets[i];
                break;
            }
        }

        if (found) {
            // 기존 탄환은 stats만 갱신 (count/level은 서버 권위 유지)
            found.stats = incoming.stats || [];
        } else {
            // 없으면 새로 추가
            bullets.push({
                bulletId: incoming.bulletId,
                level: incoming.level || 0,
                count: incoming.count || 0,
                stats: incoming.stats || []
            });
        }

        // 저장
        server.UpdateUserData({
            PlayFabId: currentPlayerId,
            Data: {
                "BulletInventory": JSON.stringify(bulletInventory)
            }
        });

        return { success: true, bulletId: incoming.bulletId };

    } catch (e) {
        log.error("UpdateBulletStats Error: " + JSON.stringify(e));
        return { error: "Server error", detail: e };
    }
};

// 연구소 스킬트리 노드 레벨 저장 (LaboratoryManager.TryEnhance 시 호출)
handlers.UpdateLabData = function (args, context) {
    try {
        if (!args.labJson)
            return { error: "Missing labJson" };

        // 형식 검증용 파싱 (그대로 저장)
        JSON.parse(args.labJson);

        server.UpdateUserData({
            PlayFabId: currentPlayerId,
            Data: {
                "LabData": args.labJson
            }
        });

        return { success: true };

    } catch (e) {
        log.error("UpdateLabData Error: " + JSON.stringify(e));
        return { error: "Server error", detail: e };
    }
};
