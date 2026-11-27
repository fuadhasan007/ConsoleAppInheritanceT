using System;
using System.Collections.Generic;
using System.Linq;

namespace PracticeProblems
{
    /// <summary>
    /// Daily Practice Problem - 2026-11-27
    /// Topic: Bank Account Hierarchy, Encapsulation, Immutable Records, Interfaces, and Polymorphic Ledger Auditing
    ///
    /// Problem Statement:
    /// Design a Banking &amp; Transaction Audit System that models multiple account types:
    /// 1. Create an immutable record 'TransactionEntry' (Timestamp, Type, Amount, BalanceAfter, Note)
    ///    to keep an accurate audit trail.
    /// 2. Define focused interfaces:
    ///    - 'ITransactable': Deposit(decimal amount, string note), TryWithdraw(decimal amount, string note, out string reason),
    ///      and read-only TransactionHistory.
    ///    - 'IAuditable': AccountRiskLevel property and GenerateAuditReport() method.
    /// 3. Create an abstract base class 'BankAccount' with validated AccountNumber, HolderName, Balance (protected set),
    ///    constructor chaining, an abstract 'CalculateMonthlyInterest()' method, and a virtual 'GetSummary()' method.
    /// 4. Implement derived account classes:
    ///    - 'SavingsAccount' (BankAccount, ITransactable): Enforces a $100 minimum balance and a monthly withdrawal cap,
    ///      and pays tiered interest (3.5% APY below $5,000; 4.5% APY at or above $5,000).
    ///    - 'CheckingAccount' (BankAccount, ITransactable, IAuditable): Supports a configurable overdraft limit with a
    ///      $25 overdraft fee when balance drops below zero, and flags overdrafts during compliance audits.
    ///    - 'FixedDepositAccount' (BankAccount, IAuditable): Locks funds for a term in months; provides higher yield
    ///      (6.0% APY) and calculates an early-closure penalty if broken before maturity.
    /// 5. Demonstrate polymorphic interest projection, transaction processing, and compliance auditing across a mixed portfolio.
    /// </summary>

    #region Supporting Types & Interfaces

    /// <summary>
    /// Represents an immutable ledger entry recorded whenever an account balance changes.
    /// </summary>
    public sealed record TransactionEntry(
        DateTime Timestamp,
        string Type,
        decimal Amount,
        decimal BalanceAfter,
        string Note);

    /// <summary>
    /// Contract for accounts that support day-to-day deposits and withdrawals.
    /// </summary>
    public interface ITransactable
    {
        IReadOnlyList<TransactionEntry> TransactionHistory { get; }
        void Deposit(decimal amount, string note = "Deposit");
        bool TryWithdraw(decimal amount, out string failureReason, string note = "Withdrawal");
    }

    /// <summary>
    /// Contract for accounts subject to periodic compliance and risk auditing.
    /// </summary>
    public interface IAuditable
    {
        string AccountRiskLevel { get; }
        string GenerateAuditReport();
    }

    #endregion

    #region Base Account Class

    /// <summary>
    /// Abstract base class for all bank accounts.
    /// Encapsulates balance mutations and ledger recording so subclasses cannot corrupt history.
    /// </summary>
    public abstract class BankAccount
    {
        private string accountNumber;
        private string holderName;
        private readonly List<TransactionEntry> ledger = new List<TransactionEntry>();

        public string AccountNumber
        {
            get => accountNumber;
            protected set => accountNumber = string.IsNullOrWhiteSpace(value) ? "ACCT-0000" : value.Trim();
        }

        public string HolderName
        {
            get => holderName;
            set => holderName = string.IsNullOrWhiteSpace(value) ? "Account Holder" : value.Trim();
        }

        public decimal Balance { get; private set; }

        public DateTime OpenedOn { get; }

        /// <summary>
        /// Exposes a read-only view of all recorded transactions for this account.
        /// </summary>
        public IReadOnlyList<TransactionEntry> TransactionHistory => ledger.AsReadOnly();

        /// <summary>
        /// Initializes a bank account with an initial opening balance.
        /// </summary>
        protected BankAccount(string accountNumber, string holderName, decimal initialBalance, DateTime? openedOn = null)
        {
            AccountNumber = accountNumber;
            HolderName = holderName;
            OpenedOn = openedOn ?? DateTime.Today;
            Balance = initialBalance >= 0m ? decimal.Round(initialBalance, 2) : 0m;

            RecordTransaction("OPEN", Balance, "Account opened");
        }

        /// <summary>
        /// Computes the projected monthly interest earned (or 0 for non-interest-bearing accounts).
        /// </summary>
        public abstract decimal CalculateMonthlyInterest();

        /// <summary>
        /// Returns a formatted single-line summary of the account state.
        /// </summary>
        public virtual string GetSummary()
        {
            return $"[{AccountNumber}] {HolderName,-18} | Balance: ${Balance,9:N2} | Monthly Interest: ${CalculateMonthlyInterest(),6:N2}";
        }

        /// <summary>
        /// Credits the account balance and appends an immutable ledger entry.
        /// </summary>
        protected void ApplyCredit(decimal amount, string type, string note)
        {
            if (amount <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Credit amount must be positive.");
            }

            Balance = decimal.Round(Balance + amount, 2);
            RecordTransaction(type, amount, note);
        }

        /// <summary>
        /// Debits the account balance and appends an immutable ledger entry.
        /// </summary>
        protected void ApplyDebit(decimal amount, string type, string note)
        {
            if (amount <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Debit amount must be positive.");
            }

            Balance = decimal.Round(Balance - amount, 2);
            RecordTransaction(type, -amount, note);
        }

        private void RecordTransaction(string type, decimal amount, string note)
        {
            ledger.Add(new TransactionEntry(DateTime.Now, type, amount, Balance, note ?? string.Empty));
        }
    }

    #endregion

    #region Derived Account Classes

    /// <summary>
    /// Interest-bearing savings account with a minimum balance requirement and a monthly withdrawal cap.
    /// </summary>
    public class SavingsAccount : BankAccount, ITransactable
    {
        public const decimal MinimumBalance = 100.00m;
        public const int MaxMonthlyWithdrawals = 3;

        private const decimal StandardApy = 0.035m;
        private const decimal HighTierApy = 0.045m;
        private const decimal HighTierThreshold = 5000.00m;

        public int WithdrawalsThisMonth { get; private set; }

        public SavingsAccount(string accountNumber, string holderName, decimal initialBalance)
            : base(accountNumber, holderName, initialBalance < MinimumBalance ? MinimumBalance : initialBalance)
        {
        }

        public void Deposit(decimal amount, string note = "Deposit")
        {
            if (amount <= 0m) return;
            ApplyCredit(amount, "DEPOSIT", note);
        }

        public bool TryWithdraw(decimal amount, out string failureReason, string note = "Withdrawal")
        {
            if (amount <= 0m)
            {
                failureReason = "Withdrawal amount must be greater than zero.";
                return false;
            }

            if (WithdrawalsThisMonth >= MaxMonthlyWithdrawals)
            {
                failureReason = $"Monthly withdrawal limit ({MaxMonthlyWithdrawals}) reached.";
                return false;
            }

            if (Balance - amount < MinimumBalance)
            {
                failureReason = $"Cannot drop below minimum balance of ${MinimumBalance:N2}.";
                return false;
            }

            ApplyDebit(amount, "WITHDRAW", note);
            WithdrawalsThisMonth++;
            failureReason = string.Empty;
            return true;
        }

        public override decimal CalculateMonthlyInterest()
        {
            decimal annualRate = Balance >= HighTierThreshold ? HighTierApy : StandardApy;
            return decimal.Round(Balance * (annualRate / 12m), 2);
        }

        public override string GetSummary()
        {
            return $"{base.GetSummary()} | Type: Savings (Withdrawals: {WithdrawalsThisMonth}/{MaxMonthlyWithdrawals})";
        }
    }

    /// <summary>
    /// Everyday checking account supporting overdraft protection with fee tracking and compliance auditing.
    /// </summary>
    public class CheckingAccount : BankAccount, ITransactable, IAuditable
    {
        public const decimal OverdraftFee = 25.00m;

        public decimal OverdraftLimit { get; }
        public int OverdraftCount { get; private set; }

        public string AccountRiskLevel
        {
            get
            {
                if (Balance < 0m || OverdraftCount >= 2) return "HIGH";
                if (OverdraftCount == 1) return "MEDIUM";
                return "LOW";
            }
        }

        public CheckingAccount(string accountNumber, string holderName, decimal initialBalance, decimal overdraftLimit = 300.00m)
            : base(accountNumber, holderName, initialBalance)
        {
            OverdraftLimit = overdraftLimit >= 0m ? overdraftLimit : 0m;
        }

        public void Deposit(decimal amount, string note = "Deposit")
        {
            if (amount <= 0m) return;
            ApplyCredit(amount, "DEPOSIT", note);
        }

        public bool TryWithdraw(decimal amount, out string failureReason, string note = "Withdrawal")
        {
            if (amount <= 0m)
            {
                failureReason = "Withdrawal amount must be greater than zero.";
                return false;
            }

            bool triggersOverdraft = (Balance - amount) < 0m;
            decimal totalCharge = triggersOverdraft ? amount + OverdraftFee : amount;

            if (Balance - totalCharge < -OverdraftLimit)
            {
                failureReason = $"Exceeds overdraft limit of ${OverdraftLimit:N2} (including ${OverdraftFee:N2} fee).";
                return false;
            }

            ApplyDebit(amount, "WITHDRAW", note);

            if (triggersOverdraft)
            {
                ApplyDebit(OverdraftFee, "FEE", "Overdraft protection fee");
                OverdraftCount++;
            }

            failureReason = string.Empty;
            return true;
        }

        public override decimal CalculateMonthlyInterest()
        {
            // Standard checking accounts do not accrue interest.
            return 0.00m;
        }

        public string GenerateAuditReport()
        {
            int largeTxCount = TransactionHistory.Count(t => Math.Abs(t.Amount) >= 1000m && t.Type != "OPEN");
            return $"Audit [{AccountNumber}] Risk={AccountRiskLevel} | Balance=${Balance:N2} | Overdrafts={OverdraftCount} | LargeTx(>=$1k)={largeTxCount}";
        }

        public override string GetSummary()
        {
            return $"{base.GetSummary()} | Type: Checking (Overdraft Limit: ${OverdraftLimit:N2})";
        }
    }

    /// <summary>
    /// Term-locked fixed deposit account. Intentionally does not implement ITransactable
    /// because routine deposits and withdrawals are prohibited until maturity.
    /// </summary>
    public sealed class FixedDepositAccount : BankAccount, IAuditable
    {
        private const decimal FixedApy = 0.060m;
        private const decimal EarlyPenaltyRate = 0.020m;

        public int TermMonths { get; }
        public DateTime MaturityDate { get; }

        public string AccountRiskLevel => Balance >= 25000m ? "MEDIUM (High Value)" : "LOW";

        public FixedDepositAccount(string accountNumber, string holderName, decimal principal, int termMonths, DateTime? openedOn = null)
            : base(accountNumber, holderName, principal, openedOn)
        {
            TermMonths = termMonths > 0 ? termMonths : 6;
            MaturityDate = OpenedOn.AddMonths(TermMonths);
        }

        public override decimal CalculateMonthlyInterest()
        {
            return decimal.Round(Balance * (FixedApy / 12m), 2);
        }

        /// <summary>
        /// Calculates the net payout if closed on the specified date, applying an early-withdrawal penalty before maturity.
        /// </summary>
        public decimal CalculatePayoutOn(DateTime asOfDate)
        {
            if (asOfDate.Date >= MaturityDate.Date)
            {
                decimal totalInterest = CalculateMonthlyInterest() * TermMonths;
                return decimal.Round(Balance + totalInterest, 2);
            }

            decimal penalty = decimal.Round(Balance * EarlyPenaltyRate, 2);
            return decimal.Round(Balance - penalty, 2);
        }

        public string GenerateAuditReport()
        {
            bool matured = DateTime.Today >= MaturityDate.Date;
            return $"Audit [{AccountNumber}] Risk={AccountRiskLevel} | Principal=${Balance:N2} | Matures={MaturityDate:yyyy-MM-dd} ({(matured ? "Matured" : "Locked")})";
        }

        public override string GetSummary()
        {
            return $"{base.GetSummary()} | Type: Fixed Deposit ({TermMonths}m, Matures {MaturityDate:yyyy-MM-dd})";
        }
    }

    #endregion

    #region Problem Solution & Verification Runner

    /// <summary>
    /// Driver class demonstrating polymorphic banking operations, transaction trails, and compliance auditing.
    /// </summary>
    public static class Day20261127Solution
    {
        /// <summary>
        /// Runs the end-to-end banking and audit scenario.
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("==================================================================");
            Console.WriteLine(" Daily Practice Problem: 2026-11-27 Solution");
            Console.WriteLine("==================================================================\n");

            var savings = new SavingsAccount("SAV-1001", "Alice Rahman", 4200.00m);
            var checking = new CheckingAccount("CHK-2001", "Bilal Karim", 500.00m, overdraftLimit: 250.00m);
            var fixedDeposit = new FixedDepositAccount("FDA-3001", "Nadia Chowdhury", 15000.00m, termMonths: 12);

            var accounts = new List<BankAccount> { savings, checking, fixedDeposit };

            Console.WriteLine("--- Initial Portfolio ---");
            foreach (BankAccount account in accounts)
            {
                Console.WriteLine(account.GetSummary());
            }

            Console.WriteLine("\n--- Processing Transactions ---");
            ProcessTransaction(savings, acct =>
            {
                acct.Deposit(1200.00m, "Freelance payout");
                AttemptWithdraw(acct, 300.00m, "Utility bills");
                AttemptWithdraw(acct, 5500.00m, "Too large (violates min balance)");
            });

            ProcessTransaction(checking, acct =>
            {
                AttemptWithdraw(acct, 600.00m, "Laptop repair (uses overdraft)");
                AttemptWithdraw(acct, 400.00m, "Exceeds overdraft limit");
                acct.Deposit(250.00m, "Reimbursement");
            });

            Console.WriteLine("\n--- Updated Portfolio & Interest Projection ---");
            decimal totalAssets = 0m;
            decimal totalMonthlyInterest = 0m;

            foreach (BankAccount account in accounts)
            {
                Console.WriteLine(account.GetSummary());
                totalAssets += account.Balance;
                totalMonthlyInterest += account.CalculateMonthlyInterest();
            }

            Console.WriteLine($"\nTotal Portfolio Balance:  ${totalAssets:N2}");
            Console.WriteLine($"Projected Monthly Yield:  ${totalMonthlyInterest:N2}");
            Console.WriteLine($"Fixed Deposit Early Exit: ${fixedDeposit.CalculatePayoutOn(DateTime.Today.AddMonths(3)):N2}");
            Console.WriteLine($"Fixed Deposit Maturity:   ${fixedDeposit.CalculatePayoutOn(fixedDeposit.MaturityDate):N2}");

            Console.WriteLine("\n--- Compliance Audit Reports ---");
            foreach (BankAccount account in accounts)
            {
                if (account is IAuditable auditable)
                {
                    Console.WriteLine("  " + auditable.GenerateAuditReport());
                }
                else
                {
                    Console.WriteLine($"  Audit [{account.AccountNumber}] Exempt from commercial risk audit (standard retail savings).");
                }
            }

            Console.WriteLine("\n--- Checking Account Ledger ---");
            foreach (TransactionEntry entry in checking.TransactionHistory)
            {
                Console.WriteLine($"  {entry.Type,-8} | {entry.Amount,9:+#,##0.00;-#,##0.00;0.00} | Bal: ${entry.BalanceAfter,8:N2} | {entry.Note}");
            }

            Console.WriteLine("\n==================================================================");
        }

        private static void ProcessTransaction(BankAccount account, Action<ITransactable> operation)
        {
            if (account is ITransactable transactable)
            {
                operation(transactable);
            }
            else
            {
                Console.WriteLine($"[{account.AccountNumber}] Transactions are not permitted on this account type.");
            }
        }

        private static void AttemptWithdraw(ITransactable account, decimal amount, string note)
        {
            if (account.TryWithdraw(amount, out string reason, note))
            {
                Console.WriteLine($"  Approved withdrawal of ${amount:N2} ({note}).");
            }
            else
            {
                Console.WriteLine($"  Declined withdrawal of ${amount:N2} ({note}): {reason}");
            }
        }
    }

    #endregion
}
