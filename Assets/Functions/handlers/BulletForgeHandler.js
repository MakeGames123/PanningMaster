// 단일 탄환 스탯 갱신 (포지에서 ApplyNewStats 시 호출)
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
