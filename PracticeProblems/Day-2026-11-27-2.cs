using System;
using System.Collections.Generic;
using System.Linq;

namespace PracticeProblems
{
    /// <summary>
    /// Daily Practice Problem - 2026-11-27 (Part 2)
    /// Topic: Generic Repositories with Type Constraints, Inheritance, Custom Exceptions, and Priority Triage
    ///
    /// Problem Statement:
    /// Design a Hospital Patient Care, Triage &amp; Billing System:
    /// 1. Define 'TriagePriority' enum (Low, Medium, High, Critical) and a custom 'CareDomainException'
    ///    for invalid clinical or billing operations.
    /// 2. Define focused interfaces:
    ///    - 'IIdentifiable': string Id { get; }
    ///    - 'IPrioritizable': TriagePriority Priority { get; } and GetTriageLabel().
    ///    - 'IInsuranceBillable': InsuranceCoverageRate and CalculateOutOfPocketCost().
    /// 3. Create an immutable record 'TreatmentCharge(string ProcedureName, decimal Cost)' and an abstract
    ///    base class 'PatientVisit' implementing IIdentifiable, IPrioritizable, and IComparable&lt;PatientVisit&gt;:
    ///    - Validates Id, PatientName, BaseConsultationFee, and tracks IsDischarged state.
    ///    - Maintains an encapsulated list of TreatmentCharge items via AddTreatment() and Discharge().
    ///    - Sorts visits by highest TriagePriority first, then earliest AdmittedAt timestamp.
    ///    - Declares abstract 'CalculateTotalBill()' and virtual 'GetCareSummary()'.
    /// 4. Implement derived visit classes:
    ///    - 'OutpatientVisit' (PatientVisit, IInsuranceBillable): Adds ClinicCopay and insurance coverage.
    ///    - 'InpatientVisit' (PatientVisit, IInsuranceBillable): Adds DaysAdmitted, DailyRoomRate, and a
    ///      5% room discount for stays of 5+ days.
    ///    - 'EmergencyVisit' (sealed, PatientVisit): Adds EmergencySurcharge and optional ambulance fee.
    /// 5. Create a generic 'CareRegistry&lt;T&gt;' constrained to 'T : PatientVisit, IIdentifiable' with duplicate
    ///    protection, predicate filtering, triage queue sorting, and total billing aggregation.
    /// </summary>

    #region Supporting Types, Enums & Exceptions

    /// <summary>
    /// Urgency levels used to order patients in the clinical triage queue.
    /// </summary>
    public enum TriagePriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// Domain-specific exception raised for invalid clinical workflow or billing operations.
    /// </summary>
    public sealed class CareDomainException : Exception
    {
        public string VisitId { get; }

        public CareDomainException(string message, string visitId = null)
            : base(message)
        {
            VisitId = visitId;
        }
    }

    /// <summary>
    /// Immutable record representing an individual medical procedure or service charge.
    /// </summary>
    public sealed record TreatmentCharge(string ProcedureName, decimal Cost);

    #endregion

    #region Interfaces

    /// <summary>
    /// Contract for domain entities identified by a unique string key.
    /// </summary>
    public interface IIdentifiable
    {
        string Id { get; }
    }

    /// <summary>
    /// Contract for entities that can be triaged by urgency level.
    /// </summary>
    public interface IPrioritizable
    {
        TriagePriority Priority { get; }
        string GetTriageLabel();
    }

    /// <summary>
    /// Contract for visits eligible for insurance cost-sharing.
    /// </summary>
    public interface IInsuranceBillable
    {
        decimal InsuranceCoverageRate { get; }
        decimal CalculateOutOfPocketCost();
    }

    #endregion

    #region Base PatientVisit Class

    /// <summary>
    /// Abstract base class representing a patient's hospital visit.
    /// Encapsulates treatment charges, discharge lifecycle, and priority-based comparison.
    /// </summary>
    public abstract class PatientVisit : IIdentifiable, IPrioritizable, IComparable<PatientVisit>
    {
        private readonly List<TreatmentCharge> charges = new List<TreatmentCharge>();
        private string patientName;
        private decimal baseConsultationFee;

        public string Id { get; }

        public string PatientName
        {
            get => patientName;
            set => patientName = string.IsNullOrWhiteSpace(value) ? "Unnamed Patient" : value.Trim();
        }

        public decimal BaseConsultationFee
        {
            get => baseConsultationFee;
            protected set
            {
                if (value < 0m)
                {
                    throw new CareDomainException("Base consultation fee cannot be negative.", Id);
                }

                baseConsultationFee = value;
            }
        }

        public TriagePriority Priority { get; }
        public DateTime AdmittedAt { get; }
        public bool IsDischarged { get; private set; }

        /// <summary>
        /// Read-only view of treatments billed during this visit.
        /// </summary>
        public IReadOnlyList<TreatmentCharge> Charges => charges.AsReadOnly();

        /// <summary>
        /// Convenience constructor chaining to the full constructor using the current timestamp.
        /// </summary>
        protected PatientVisit(string id, string patientName, TriagePriority priority, decimal baseConsultationFee)
            : this(id, patientName, priority, baseConsultationFee, DateTime.Now)
        {
        }

        /// <summary>
        /// Full constructor initializing core visit identity, triage priority, and fee.
        /// </summary>
        protected PatientVisit(
            string id,
            string patientName,
            TriagePriority priority,
            decimal baseConsultationFee,
            DateTime admittedAt)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new CareDomainException("Visit ID cannot be empty.");
            }

            Id = id.Trim().ToUpperInvariant();
            PatientName = patientName;
            Priority = priority;
            BaseConsultationFee = baseConsultationFee;
            AdmittedAt = admittedAt;
        }

        /// <summary>
        /// Records a treatment charge on an active (non-discharged) visit.
        /// </summary>
        public void AddTreatment(string procedureName, decimal cost)
        {
            if (IsDischarged)
            {
                throw new CareDomainException($"Cannot add treatment '{procedureName}' after patient discharge.", Id);
            }

            if (string.IsNullOrWhiteSpace(procedureName))
            {
                throw new CareDomainException("Procedure name must not be empty.", Id);
            }

            if (cost <= 0m)
            {
                throw new CareDomainException($"Treatment cost for '{procedureName}' must be positive.", Id);
            }

            charges.Add(new TreatmentCharge(procedureName.Trim(), cost));
        }

        /// <summary>
        /// Marks the visit as discharged, locking further treatment additions.
        /// </summary>
        public void Discharge()
        {
            if (IsDischarged)
            {
                throw new CareDomainException("Patient visit is already discharged.", Id);
            }

            IsDischarged = true;
        }

        /// <summary>
        /// Sums all recorded procedure charges for this visit.
        /// </summary>
        protected decimal GetTreatmentsSubtotal()
        {
            decimal subtotal = 0m;
            foreach (TreatmentCharge charge in charges)
            {
                subtotal += charge.Cost;
            }

            return subtotal;
        }

        /// <summary>
        /// Orders visits by highest TriagePriority first, breaking ties by earliest admission time.
        /// </summary>
        public int CompareTo(PatientVisit other)
        {
            if (other is null)
            {
                return -1;
            }

            int priorityComparison = other.Priority.CompareTo(Priority);
            return priorityComparison != 0
                ? priorityComparison
                : AdmittedAt.CompareTo(other.AdmittedAt);
        }

        public string GetTriageLabel() => Priority switch
        {
            TriagePriority.Critical => "RED - Immediate",
            TriagePriority.High => "ORANGE - Urgent",
            TriagePriority.Medium => "YELLOW - Standard",
            _ => "GREEN - Routine"
        };

        /// <summary>
        /// Computes the gross total bill before any insurance adjustment.
        /// </summary>
        public abstract decimal CalculateTotalBill();

        /// <summary>
        /// Formats a human-readable summary of the patient visit.
        /// </summary>
        public virtual string GetCareSummary()
        {
            string status = IsDischarged ? "Discharged" : "Active";
            return $"[{Id}] {PatientName,-16} | {GetTriageLabel(),-17} | {status,-10} | Treatments: {charges.Count,2} | Gross Bill: ${CalculateTotalBill(),8:N2}";
        }
    }

    #endregion

    #region Derived Visit Classes

    /// <summary>
    /// Standard clinic consultation eligible for insurance coverage.
    /// </summary>
    public class OutpatientVisit : PatientVisit, IInsuranceBillable
    {
        public decimal ClinicCopay { get; }
        public decimal InsuranceCoverageRate { get; }

        public OutpatientVisit(
            string id,
            string patientName,
            TriagePriority priority,
            decimal baseConsultationFee,
            decimal clinicCopay,
            decimal insuranceCoverageRate,
            DateTime? admittedAt = null)
            : base(id, patientName, priority, baseConsultationFee, admittedAt ?? DateTime.Now)
        {
            if (clinicCopay < 0m)
            {
                throw new CareDomainException("Clinic copay cannot be negative.", Id);
            }

            if (insuranceCoverageRate < 0m || insuranceCoverageRate > 0.90m)
            {
                throw new CareDomainException("Insurance coverage rate must be between 0.00 and 0.90.", Id);
            }

            ClinicCopay = clinicCopay;
            InsuranceCoverageRate = insuranceCoverageRate;
        }

        public override decimal CalculateTotalBill()
        {
            return BaseConsultationFee + GetTreatmentsSubtotal() + ClinicCopay;
        }

        public decimal CalculateOutOfPocketCost()
        {
            decimal total = CalculateTotalBill();
            return decimal.Round(total * (1m - InsuranceCoverageRate), 2);
        }

        public override string GetCareSummary()
        {
            return $"{base.GetCareSummary()} | Outpatient (OOP: ${CalculateOutOfPocketCost():N2})";
        }
    }

    /// <summary>
    /// Multi-day hospital admission with room charges, long-stay discount, and insurance support.
    /// </summary>
    public class InpatientVisit : PatientVisit, IInsuranceBillable
    {
        public int DaysAdmitted { get; }
        public decimal DailyRoomRate { get; }
        public decimal InsuranceCoverageRate { get; }

        public InpatientVisit(
            string id,
            string patientName,
            TriagePriority priority,
            decimal baseConsultationFee,
            int daysAdmitted,
            decimal dailyRoomRate,
            decimal insuranceCoverageRate,
            DateTime? admittedAt = null)
            : base(id, patientName, priority, baseConsultationFee, admittedAt ?? DateTime.Now)
        {
            if (daysAdmitted < 1)
            {
                throw new CareDomainException("Inpatient stay must be at least 1 day.", Id);
            }

            if (dailyRoomRate < 0m)
            {
                throw new CareDomainException("Daily room rate cannot be negative.", Id);
            }

            if (insuranceCoverageRate < 0m || insuranceCoverageRate > 0.90m)
            {
                throw new CareDomainException("Insurance coverage rate must be between 0.00 and 0.90.", Id);
            }

            DaysAdmitted = daysAdmitted;
            DailyRoomRate = dailyRoomRate;
            InsuranceCoverageRate = insuranceCoverageRate;
        }

        public decimal CalculateRoomCharges()
        {
            decimal grossRoom = DaysAdmitted * DailyRoomRate;
            return DaysAdmitted >= 5 ? decimal.Round(grossRoom * 0.95m, 2) : grossRoom;
        }

        public override decimal CalculateTotalBill()
        {
            return BaseConsultationFee + GetTreatmentsSubtotal() + CalculateRoomCharges();
        }

        public decimal CalculateOutOfPocketCost()
        {
            decimal total = CalculateTotalBill();
            return decimal.Round(total * (1m - InsuranceCoverageRate), 2);
        }

        public override string GetCareSummary()
        {
            string discountTag = DaysAdmitted >= 5 ? " (5% Stay Disc.)" : string.Empty;
            return $"{base.GetCareSummary()} | Inpatient {DaysAdmitted}d{discountTag} (OOP: ${CalculateOutOfPocketCost():N2})";
        }
    }

    /// <summary>
    /// Sealed emergency department visit that applies trauma surcharges and optional ambulance dispatch fees.
    /// </summary>
    public sealed class EmergencyVisit : PatientVisit
    {
        private const decimal TraumaSurcharge = 150.00m;
        private const decimal AmbulanceDispatchFee = 220.00m;

        public bool ArrivedByAmbulance { get; }

        public EmergencyVisit(
            string id,
            string patientName,
            TriagePriority priority,
            decimal baseConsultationFee,
            bool arrivedByAmbulance,
            DateTime? admittedAt = null)
            : base(id, patientName, priority, baseConsultationFee, admittedAt ?? DateTime.Now)
        {
            ArrivedByAmbulance = arrivedByAmbulance;
        }

        public override decimal CalculateTotalBill()
        {
            decimal transportFee = ArrivedByAmbulance ? AmbulanceDispatchFee : 0m;
            return BaseConsultationFee + GetTreatmentsSubtotal() + TraumaSurcharge + transportFee;
        }

        public override string GetCareSummary()
        {
            string transport = ArrivedByAmbulance ? "Ambulance" : "Walk-in";
            return $"{base.GetCareSummary()} | Emergency ({transport})";
        }
    }

    #endregion

    #region Generic Care Registry

    /// <summary>
    /// Generic repository for managing patient visits with compile-time type constraints.
    /// </summary>
    /// <typeparam name="T">A visit type deriving from PatientVisit and implementing IIdentifiable.</typeparam>
    public class CareRegistry<T> where T : PatientVisit, IIdentifiable
    {
        private readonly Dictionary<string, T> visitsById = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);

        public int Count => visitsById.Count;

        /// <summary>
        /// Registers a new visit while preventing duplicate visit identifiers.
        /// </summary>
        public void Register(T visit)
        {
            if (visit is null)
            {
                throw new CareDomainException("Cannot register a null patient visit.");
            }

            if (visitsById.ContainsKey(visit.Id))
            {
                throw new CareDomainException($"A visit with ID '{visit.Id}' is already registered.", visit.Id);
            }

            visitsById.Add(visit.Id, visit);
        }

        /// <summary>
        /// Retrieves a registered visit by ID or returns null if not found.
        /// </summary>
        public T FindById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            visitsById.TryGetValue(id.Trim(), out T visit);
            return visit;
        }

        /// <summary>
        /// Returns all visits ordered by TriagePriority (descending) and admission time (ascending).
        /// </summary>
        public List<T> GetSortedTriageQueue()
        {
            List<T> sorted = new List<T>(visitsById.Values);
            sorted.Sort();
            return sorted;
        }

        /// <summary>
        /// Filters visits matching the caller-supplied predicate.
        /// </summary>
        public List<T> GetWhere(Func<T, bool> predicate)
        {
            if (predicate is null)
            {
                return new List<T>(visitsById.Values);
            }

            return visitsById.Values.Where(predicate).ToList();
        }

        /// <summary>
        /// Aggregates total gross billing across all registered visits.
        /// </summary>
        public decimal CalculateTotalGrossBilling()
        {
            decimal total = 0m;
            foreach (T visit in visitsById.Values)
            {
                total += visit.CalculateTotalBill();
            }

            return total;
        }
    }

    #endregion

    #region Solution Runner

    /// <summary>
    /// Demonstrates generic repository constraints, custom exception handling, triage sorting, and billing.
    /// </summary>
    public static class Day20261127Part2Solution
    {
        public static void Run()
        {
            Console.WriteLine("==========================================================================");
            Console.WriteLine("  Daily C# Practice (2026-11-27 Part 2) - Hospital Care & Triage System");
            Console.WriteLine("==========================================================================\n");

            CareRegistry<PatientVisit> registry = new CareRegistry<PatientVisit>();
            DateTime baseTime = new DateTime(2026, 11, 27, 8, 30, 0);

            OutpatientVisit outpatient = new OutpatientVisit(
                "VIS-101", "Amina Rahman", TriagePriority.Low,
                baseConsultationFee: 75.00m, clinicCopay: 20.00m, insuranceCoverageRate: 0.70m,
                admittedAt: baseTime);
            outpatient.AddTreatment("Blood Panel", 65.00m);

            InpatientVisit inpatient = new InpatientVisit(
                "VIS-102", "Tariq Mahmud", TriagePriority.High,
                baseConsultationFee: 150.00m, daysAdmitted: 6, dailyRoomRate: 320.00m, insuranceCoverageRate: 0.80m,
                admittedAt: baseTime.AddMinutes(25));
            inpatient.AddTreatment("IV Antibiotics", 240.00m);
            inpatient.AddTreatment("Chest X-Ray", 180.00m);

            EmergencyVisit emergencyWalkIn = new EmergencyVisit(
                "VIS-103", "Nadia Islam", TriagePriority.High,
                baseConsultationFee: 180.00m, arrivedByAmbulance: false,
                admittedAt: baseTime.AddMinutes(10));
            emergencyWalkIn.AddTreatment("Suturing & Dressing", 130.00m);

            EmergencyVisit emergencyCritical = new EmergencyVisit(
                "VIS-104", "Kamal Hossain", TriagePriority.Critical,
                baseConsultationFee: 220.00m, arrivedByAmbulance: true,
                admittedAt: baseTime.AddMinutes(40));
            emergencyCritical.AddTreatment("ECG & Cardiac Enzymes", 310.00m);
            emergencyCritical.AddTreatment("CT Scan", 540.00m);

            registry.Register(outpatient);
            registry.Register(inpatient);
            registry.Register(emergencyWalkIn);
            registry.Register(emergencyCritical);

            // Discharge the routine outpatient visit
            outpatient.Discharge();

            Console.WriteLine("--- Domain Validation & Exception Handling ---");
            TryExecute(() => registry.Register(outpatient), "Register duplicate visit VIS-101");
            TryExecute(() => outpatient.AddTreatment("Follow-up Bandage", 15.00m), "Add treatment to discharged visit");
            TryExecute(() => emergencyCritical.AddTreatment("Invalid Free Entry", -10.00m), "Add negative treatment cost");

            Console.WriteLine("\n--- Sorted Clinical Triage Queue (Highest Priority, Earliest Arrival) ---");
            foreach (PatientVisit visit in registry.GetSortedTriageQueue())
            {
                Console.WriteLine("  " + visit.GetCareSummary());
            }

            Console.WriteLine("\n--- Insurance Out-of-Pocket Breakdown ---");
            foreach (PatientVisit visit in registry.GetSortedTriageQueue())
            {
                if (visit is IInsuranceBillable billable)
                {
                    decimal gross = visit.CalculateTotalBill();
                    decimal outOfPocket = billable.CalculateOutOfPocketCost();
                    decimal covered = gross - outOfPocket;
                    Console.WriteLine($"  [{visit.Id}] {visit.PatientName,-16} | Coverage: {billable.InsuranceCoverageRate:P0} | Covered: ${covered,8:N2} | Patient Pays: ${outOfPocket,8:N2}");
                }
                else
                {
                    Console.WriteLine($"  [{visit.Id}] {visit.PatientName,-16} | Direct/Emergency Billing | Patient Pays: ${visit.CalculateTotalBill(),8:N2}");
                }
            }

            List<PatientVisit> activeUrgentCases = registry.GetWhere(v => !v.IsDischarged && v.Priority >= TriagePriority.High);
            Console.WriteLine($"\nActive High/Critical Visits: {activeUrgentCases.Count} of {registry.Count}");
            Console.WriteLine($"Total Gross Department Billing: ${registry.CalculateTotalGrossBilling():N2}");
            Console.WriteLine("\n==========================================================================");
        }

        private static void TryExecute(Action action, string operationLabel)
        {
            try
            {
                action();
            }
            catch (CareDomainException ex)
            {
                Console.WriteLine($"  Blocked [{operationLabel}] -> {ex.Message}");
            }
        }
    }

    #endregion
}
