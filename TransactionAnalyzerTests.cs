using TransactionLib;

public class TransactionAnalyzerTests
{
    [Fact]
    public void RejectsUnknownTransactionType()
    {
        var tx = new Transaction
        {
            Amount = 100,
            Kind = TransactionKind.Unknown,
            Timestamp = DateTime.Now
        };

        Assert.StartsWith("Ошибка", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void RejectsTooManyTransactionsForNonVip()
    {
        var tx = new Transaction
        {
            Amount = 100,
            Kind = TransactionKind.Deposit,
            DailyTransactionCount = 15,
            IsVipClient = false
        };

        Assert.Contains("превышено количество", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void AllowsVipToTransferLargeExternal()
    {
        var tx = new Transaction
        {
            Amount = 2_000_000,
            Kind = TransactionKind.Transfer,
            IsInternal = false,
            IsVipClient = true
        };

        Assert.Equal("Транзакция допустима.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void AppliesCommissionForNonVipCurrentToSaving()
    {
        var tx = new Transaction
        {
            Amount = 1000,
            Kind = TransactionKind.Transfer,
            FromAccountType = "Текущий",
            ToAccountType = "Сберегательный",
            IsInternal = false,
            IsVipClient = false
        };

        Assert.StartsWith("Комиссия", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void RejectsDailySumExceededForNonVip()
    {
        var tx = new Transaction
        {
            Amount = 300_000,
            Kind = TransactionKind.Transfer,
            DailyTransactionTotal = 1_300_000,
            IsVipClient = false
        };

        Assert.StartsWith("Ограничение: превышена", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void RejectsLargeAtmTransfer()
    {
        var tx = new Transaction
        {
            Amount = 150_000,
            Kind = TransactionKind.Transfer,
            Channel = "ATM",
            IsInternal = true
        };

        Assert.StartsWith("Ограничение: банкоматы", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void AllowsVipLargeExternalWithdrawal()
    {
        var tx = new Transaction
        {
            Amount = 500_000,
            Kind = TransactionKind.Withdrawal,
            IsInternal = false,
            IsVipClient = true,
            Channel = "Office"
        };

        Assert.Equal("Транзакция допустима.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void RejectsZeroAmount()
    {
        var tx = new Transaction
        {
            Amount = 0,
            Kind = TransactionKind.Unknown,
            IsVipClient = false,
            Channel = "Web",
            Timestamp = DateTime.Now
        };

        Assert.StartsWith("Ошибка", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    // ===================================================================
    // Добавлены по итогам мутационного тестирования (ЛР №9)
    // ===================================================================

    [Fact]
    public void ZeroAmountReturnsAmountError()
    {
        var tx = new Transaction
        {
            Amount = 0m,
            Kind = TransactionKind.Unknown,
        };

        Assert.Equal("Ошибка: сумма должна быть положительной.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void UnknownKindReturnsExactMessage()
    {
        var tx = new Transaction
        {
            Amount = 100m,
            Kind = TransactionKind.Unknown,
        };

        Assert.Equal("Ошибка: неизвестный тип транзакции.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void DailyCountEqualTenIsAllowed()
    {
        var tx = new Transaction
        {
            Amount = 100m,
            Kind = TransactionKind.Deposit,
            DailyTransactionCount = 10,
        };

        Assert.Equal("Транзакция допустима.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void DailyCountElevenRejectedWithExactMessage()
    {
        var tx = new Transaction
        {
            Amount = 100m,
            Kind = TransactionKind.Deposit,
            DailyTransactionCount = 11,
        };

        Assert.Equal("Ограничение: превышено количество операций в день.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void DailyTotalExactlyAtLimitAllowed()
    {
        var tx = new Transaction
        {
            Amount = 500_000m,
            Kind = TransactionKind.Deposit,
            DailyTransactionTotal = 1_000_000m,
        };

        Assert.Equal("Транзакция допустима.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void DailyTotalOneOverLimitRejected()
    {
        var tx = new Transaction
        {
            Amount = 500_001m,
            Kind = TransactionKind.Deposit,
            DailyTransactionTotal = 1_000_000m,
        };

        Assert.Equal("Ограничение: превышена дневная сумма транзакций.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void AtmExactlyHundredThousandAllowed()
    {
        var tx = new Transaction
        {
            Amount = 100_000m,
            Kind = TransactionKind.Deposit,
            Channel = "ATM",
        };

        Assert.Equal("Транзакция допустима.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void AtmAboveLimitRejectedExactMessage()
    {
        var tx = new Transaction
        {
            Amount = 100_001m,
            Kind = TransactionKind.Deposit,
            Channel = "ATM",
        };

        Assert.Equal("Ограничение: банкоматы не обрабатывают крупные суммы.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void NonVipExternalTransferOverMillionRejected()
    {
        var tx = new Transaction
        {
            Amount = 1_000_001m,
            Kind = TransactionKind.Transfer,
            IsInternal = false,
            FromAccountType = "Текущий",
            ToAccountType = "Текущий",
        };

        Assert.Equal("Отклонено: крупные внешние переводы недопустимы.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void NonVipExternalTransferExactlyMillionNotRejectedAsLarge()
    {
        var tx = new Transaction
        {
            Amount = 1_000_000m,
            Kind = TransactionKind.Transfer,
            IsInternal = false,
            FromAccountType = "Текущий",
            ToAccountType = "Текущий",
        };

        Assert.Equal("Транзакция допустима.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void CommissionExactMessage()
    {
        var tx = new Transaction
        {
            Amount = 1_000m,
            Kind = TransactionKind.Transfer,
            FromAccountType = "Текущий",
            ToAccountType = "Сберегательный",
        };

        Assert.Equal("Комиссия: 1% за перевод между типами счетов.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void NoCommissionForInternalTransferDifferentTypes()
    {
        var tx = new Transaction
        {
            Amount = 1_000m,
            Kind = TransactionKind.Transfer,
            IsInternal = true,
            FromAccountType = "Текущий",
            ToAccountType = "Сберегательный",
            Timestamp = new DateTime(2026, 10, 12),
            Channel = "Office",
        };

        Assert.Equal("Транзакция допустима.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void NoCommissionForVipDifferentTypes()
    {
        var tx = new Transaction
        {
            Amount = 1_000m,
            Kind = TransactionKind.Transfer,
            FromAccountType = "Текущий",
            ToAccountType = "Сберегательный",
            IsVipClient = true,
            Timestamp = new DateTime(2026, 10, 12),
        };

        Assert.Equal("Транзакция допустима.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void WeekendSaturdayMobileInternalDifferentTypesRejected()
    {
        var tx = new Transaction
        {
            Amount = 1_000m,
            Kind = TransactionKind.Transfer,
            IsInternal = true,
            FromAccountType = "Текущий",
            ToAccountType = "Сберегательный",
            Timestamp = new DateTime(2026, 10, 17),
            Channel = "Mobile",
        };

        Assert.Equal("Ошибка: в выходные нельзя переводить между разными типами счетов через онлайн-каналы.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void WeekendSundayWebInternalDifferentTypesRejected()
    {
        var tx = new Transaction
        {
            Amount = 1_000m,
            Kind = TransactionKind.Transfer,
            IsInternal = true,
            FromAccountType = "Текущий",
            ToAccountType = "Сберегательный",
            Timestamp = new DateTime(2026, 10, 18),
            Channel = "Web",
        };

        Assert.Equal("Ошибка: в выходные нельзя переводить между разными типами счетов через онлайн-каналы.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void WeekendOnlineSameTypesAllowed()
    {
        var tx = new Transaction
        {
            Amount = 1_000m,
            Kind = TransactionKind.Transfer,
            IsInternal = true,
            FromAccountType = "Текущий",
            ToAccountType = "Текущий",
            Timestamp = new DateTime(2026, 10, 17),
            Channel = "Web",
        };

        Assert.Equal("Транзакция допустима.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void WebWithdrawalRejected()
    {
        var tx = new Transaction
        {
            Amount = 1_000m,
            Kind = TransactionKind.Withdrawal,
            IsInternal = true,
            Channel = "Web",
        };

        Assert.Equal("Отклонено: снятие через веб-банкинг запрещено.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void NonVipExternalWithdrawalOver200kRejected()
    {
        var tx = new Transaction
        {
            Amount = 200_001m,
            Kind = TransactionKind.Withdrawal,
            IsInternal = false,
            Channel = "Office",
        };

        Assert.Equal("Ограничение: крупное снятие только для VIP-клиентов.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }

    [Fact]
    public void NonVipExternalWithdrawalExactly200kAllowed()
    {
        var tx = new Transaction
        {
            Amount = 200_000m,
            Kind = TransactionKind.Withdrawal,
            IsInternal = false,
            Channel = "Office",
        };

        Assert.Equal("Транзакция допустима.", TransactionAnalyzer.AnalyzeTransaction(tx));
    }
}
