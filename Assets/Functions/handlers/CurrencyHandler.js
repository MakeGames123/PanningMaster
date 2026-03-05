handlers.UsePlayerGold = function (args, context) {
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
};