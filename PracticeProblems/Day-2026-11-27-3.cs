using System;
using System.Collections.Generic;
using System.Linq;

namespace PracticeProblems
{
    /// <summary>
    /// Daily Practice Problem - 2026-11-27 (Part 3)
    /// Topic: Custom Value Types with Operator Overloading, Strategy Delegates, Extension Methods, Interfaces, and Inheritance
    ///
    /// Problem Statement:
    /// Design a Freight &amp; Logistics Dispatch System:
    /// 1. Create a readonly value type 'CargoWeight' implementing IEquatable&lt;CargoWeight&gt; and IComparable&lt;CargoWeight&gt;:
    ///    - Validates non-negative kilograms, exposes MetricTons, and provides a FromTons() factory method.
    ///    - Overloads +, ==, !=, &lt;, &gt;, &lt;=, and &gt;= operators for natural weight arithmetic and comparisons.
    /// 2. Define a delegate 'SurchargePolicy(Shipment shipment, decimal baseFreight)' to support pluggable pricing rules,
    ///    along with focused interfaces:
    ///    - 'IDispatchable': IsDispatched, DispatchedAt, Checkpoints, Dispatch(), RecordCheckpoint().
    ///    - 'IInsuredCargo': DeclaredValue and CalculateInsurancePremium().
    /// 3. Create an abstract base class 'Shipment' implementing IDispatchable:
    ///    - Validated properties: TrackingCode, DestinationCity, Weight (CargoWeight), Priority, BaseRatePerKg.
    ///    - Encapsulated checkpoint history using an immutable 'DispatchCheckpoint' record.
    ///    - Abstract 'CalculateFreightCost(SurchargePolicy customPolicy = null)' and virtual 'GetManifestLine()'.
    /// 4. Implement derived shipment types:
    ///    - 'GroundFreightShipment' (Shipment): Distance-based rate multiplier and optional liftgate fee.
    ///    - 'AirCargoShipment' (Shipment, IInsuredCargo): Expedited air multiplier, minimum floor fee, hazmat surcharge,
    ///      and cargo insurance calculation.
    ///    - 'RefrigeratedShipment' (sealed, Shipment, IInsuredCargo): Temperature-controlled cold-chain hourly cooling fee
    ///      and spoilage-adjusted insurance premium.
    /// 5. Implement 'ShipmentExtensions' extension methods (TotalCargoWeight, FilterByMinWeight, ToDisplayWeight)
    ///    and a 'DispatchManifest' coordinator that accepts an Action&lt;string&gt; audit logger, applies SurchargePolicy
    ///    delegates, and summarizes shipments by priority using LINQ.
    /// </summary>

    #region Supporting Types, Enums, Records & Delegates

    /// <summary>
    /// Service tier for freight routing and handling.
    /// </summary>
    public enum ShipmentPriority
    {
        Economy = 1,
        Standard = 2,
        Express = 3,
        ColdChain = 4
    }

    /// <summary>
    /// Immutable audit checkpoint recorded as a shipment moves between hubs.
    /// </summary>
    public sealed record DispatchCheckpoint(DateTime Timestamp, string HubCode, string StatusNote);

    /// <summary>
    /// Delegate representing a pluggable pricing surcharge rule applied on top of base freight.
    /// </summary>
    public delegate decimal SurchargePolicy(Shipment shipment, decimal baseFreight);

    /// <summary>
    /// Immutable value type representing cargo weight in kilograms with operator overloading.
    /// </summary>
    public readonly struct CargoWeight : IEquatable<CargoWeight>, IComparable<CargoWeight>
    {
        public static readonly CargoWeight Zero = new CargoWeight(0m);

        public decimal Kilograms { get; }
        public decimal MetricTons => decimal.Round(Kilograms / 1000m, 4);

        public CargoWeight(decimal kilograms)
        {
            if (kilograms < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(kilograms), "Cargo weight cannot be negative.");
            }

            Kilograms = decimal.Round(kilograms, 2);
        }

        public static CargoWeight FromTons(decimal metricTons)
        {
            if (metricTons < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(metricTons), "Metric tons cannot be negative.");
            }

            return new CargoWeight(metricTons * 1000m);
        }

        public static CargoWeight operator +(CargoWeight left, CargoWeight right)
            => new CargoWeight(left.Kilograms + right.Kilograms);

        public static bool operator ==(CargoWeight left, CargoWeight right) => left.Equals(right);
        public static bool operator !=(CargoWeight left, CargoWeight right) => !left.Equals(right);
        public static bool operator <(CargoWeight left, CargoWeight right) => left.Kilograms < right.Kilograms;
        public static bool operator >(CargoWeight left, CargoWeight right) => left.Kilograms > right.Kilograms;
        public static bool operator <=(CargoWeight left, CargoWeight right) => left.Kilograms <= right.Kilograms;
        public static bool operator >=(CargoWeight left, CargoWeight right) => left.Kilograms >= right.Kilograms;

        public int CompareTo(CargoWeight other) => Kilograms.CompareTo(other.Kilograms);

        public bool Equals(CargoWeight other) => Kilograms == other.Kilograms;

        public override bool Equals(object obj) => obj is CargoWeight other && Equals(other);

        public override int GetHashCode() => Kilograms.GetHashCode();

        public override string ToString() => $"{Kilograms:N2} kg";
    }

    #endregion

    #region Interfaces

    /// <summary>
    /// Contract for shipments that can be dispatched and tracked across transit hubs.
    /// </summary>
    public interface IDispatchable
    {
        bool IsDispatched { get; }
        DateTime? DispatchedAt { get; }
        IReadOnlyList<DispatchCheckpoint> Checkpoints { get; }
        void Dispatch(string originHub, DateTime? timestamp = null);
        void RecordCheckpoint(string hubCode, string statusNote, DateTime? timestamp = null);
    }

    /// <summary>
    /// Contract for high-value or sensitive shipments carrying cargo insurance.
    /// </summary>
    public interface IInsuredCargo
    {
        decimal DeclaredValue { get; }
        decimal CalculateInsurancePremium();
    }

    #endregion

    #region Base Shipment Class

    /// <summary>
    /// Abstract base class for all freight shipments.
    /// Encapsulates validation, dispatch lifecycle, and transit checkpoint history.
    /// </summary>
    public abstract class Shipment : IDispatchable
    {
        private readonly List<DispatchCheckpoint> checkpoints = new List<DispatchCheckpoint>();
        private string destinationCity;
        private decimal baseRatePerKg;

        public string TrackingCode { get; }

        public string DestinationCity
        {
            get => destinationCity;
            set => destinationCity = string.IsNullOrWhiteSpace(value) ? "Unspecified Hub" : value.Trim();
        }

        public CargoWeight Weight { get; }

        public ShipmentPriority Priority { get; }

        public decimal BaseRatePerKg
        {
            get => baseRatePerKg;
            protected set => baseRatePerKg = value > 0m ? decimal.Round(value, 2) : 1.00m;
        }

        public bool IsDispatched { get; private set; }

        public DateTime? DispatchedAt { get; private set; }

        public IReadOnlyList<DispatchCheckpoint> Checkpoints => checkpoints.AsReadOnly();

        protected Shipment(
            string trackingCode,
            string destinationCity,
            CargoWeight weight,
            ShipmentPriority priority,
            decimal baseRatePerKg)
        {
            if (string.IsNullOrWhiteSpace(trackingCode))
            {
                throw new ArgumentException("Tracking code cannot be empty.", nameof(trackingCode));
            }

            if (weight.Kilograms <= 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(weight), "Shipment weight must be greater than zero.");
            }

            TrackingCode = trackingCode.Trim().ToUpperInvariant();
            DestinationCity = destinationCity;
            Weight = weight;
            Priority = priority;
            BaseRatePerKg = baseRatePerKg;
        }

        /// <summary>
        /// Marks the shipment as dispatched from an origin hub and logs the initial checkpoint.
        /// </summary>
        public void Dispatch(string originHub, DateTime? timestamp = null)
        {
            if (IsDispatched)
            {
                throw new InvalidOperationException($"Shipment '{TrackingCode}' has already been dispatched.");
            }

            DateTime eventTime = timestamp ?? DateTime.Now;
            IsDispatched = true;
            DispatchedAt = eventTime;
            RecordCheckpoint(originHub, $"Dispatched toward {DestinationCity}", eventTime);
        }

        /// <summary>
        /// Appends an immutable hub checkpoint to the shipment's transit history.
        /// </summary>
        public void RecordCheckpoint(string hubCode, string statusNote, DateTime? timestamp = null)
        {
            string normalizedHub = string.IsNullOrWhiteSpace(hubCode) ? "HUB-UNK" : hubCode.Trim().ToUpperInvariant();
            string normalizedNote = string.IsNullOrWhiteSpace(statusNote) ? "Status updated" : statusNote.Trim();
            checkpoints.Add(new DispatchCheckpoint(timestamp ?? DateTime.Now, normalizedHub, normalizedNote));
        }

        /// <summary>
        /// Computes total freight cost, optionally applying a custom surcharge policy delegate.
        /// </summary>
        public abstract decimal CalculateFreightCost(SurchargePolicy customPolicy = null);

        /// <summary>
        /// Helper used by subclasses to apply an optional surcharge delegate cleanly.
        /// </summary>
        protected decimal ApplyPolicy(decimal baseFreight, SurchargePolicy customPolicy)
        {
            decimal surcharge = customPolicy != null ? customPolicy(this, baseFreight) : 0m;
            return decimal.Round(baseFreight + Math.Max(0m, surcharge), 2);
        }

        /// <summary>
        /// Returns a formatted single-line manifest description for reporting.
        /// </summary>
        public virtual string GetManifestLine()
        {
            string dispatchState = IsDispatched ? "IN TRANSIT" : "STAGED";
            return $"[{TrackingCode}] {GetType().Name,-22} | {DestinationCity,-14} | {Weight.Kilograms,8:N2} kg | {Priority,-9} | {dispatchState,-10} | Cost: ${CalculateFreightCost(),8:N2}";
        }
    }

    #endregion

    #region Derived Shipment Classes

    /// <summary>
    /// Overland truck shipment priced by weight, haul distance, and optional liftgate service.
    /// </summary>
    public class GroundFreightShipment : Shipment
    {
        public int DistanceKm { get; }
        public bool RequiresLiftgate { get; }

        public GroundFreightShipment(
            string trackingCode,
            string destinationCity,
            CargoWeight weight,
            int distanceKm,
            bool requiresLiftgate = false,
            decimal baseRatePerKg = 1.40m,
            ShipmentPriority priority = ShipmentPriority.Standard)
            : base(trackingCode, destinationCity, weight, priority, baseRatePerKg)
        {
            DistanceKm = distanceKm > 0 ? distanceKm : 1;
            RequiresLiftgate = requiresLiftgate;
        }

        public override decimal CalculateFreightCost(SurchargePolicy customPolicy = null)
        {
            decimal distanceFactor = DistanceKm switch
            {
                <= 250 => 1.00m,
                <= 800 => 1.25m,
                _ => 1.50m
            };

            decimal baseFreight = Weight.Kilograms * BaseRatePerKg * distanceFactor;
            if (RequiresLiftgate)
            {
                baseFreight += 35.00m;
            }

            return ApplyPolicy(baseFreight, customPolicy);
        }

        public override string GetManifestLine()
        {
            string liftgateTag = RequiresLiftgate ? "Liftgate" : "Dock";
            return $"{base.GetManifestLine()} | {DistanceKm} km ({liftgateTag})";
        }
    }

    /// <summary>
    /// Expedited air cargo shipment supporting hazardous material handling and cargo insurance.
    /// </summary>
    public class AirCargoShipment : Shipment, IInsuredCargo
    {
        private const decimal MinimumAirCharge = 95.00m;
        private const decimal AirMultiplier = 1.85m;

        public decimal DeclaredValue { get; }
        public bool IsHazardous { get; }

        public AirCargoShipment(
            string trackingCode,
            string destinationCity,
            CargoWeight weight,
            decimal declaredValue,
            bool isHazardous = false,
            decimal baseRatePerKg = 3.60m)
            : base(trackingCode, destinationCity, weight, ShipmentPriority.Express, baseRatePerKg)
        {
            DeclaredValue = declaredValue >= 0m ? decimal.Round(declaredValue, 2) : 0m;
            IsHazardous = isHazardous;
        }

        public decimal CalculateInsurancePremium()
        {
            if (DeclaredValue <= 0m)
            {
                return 0m;
            }

            return Math.Max(15.00m, decimal.Round(DeclaredValue * 0.012m, 2));
        }

        public override decimal CalculateFreightCost(SurchargePolicy customPolicy = null)
        {
            decimal computed = Weight.Kilograms * BaseRatePerKg * AirMultiplier;
            decimal baseFreight = Math.Max(MinimumAirCharge, computed);

            if (IsHazardous)
            {
                baseFreight *= 1.25m;
            }

            return ApplyPolicy(baseFreight, customPolicy);
        }

        public override string GetManifestLine()
        {
            string hazTag = IsHazardous ? "HAZMAT" : "General Air";
            return $"{base.GetManifestLine()} | {hazTag}, Ins: ${CalculateInsurancePremium():N2}";
        }
    }

    /// <summary>
    /// Sealed temperature-controlled shipment for pharmaceuticals and perishables.
    /// </summary>
    public sealed class RefrigeratedShipment : Shipment, IInsuredCargo
    {
        public double TargetTemperatureCelsius { get; }
        public int TransitHours { get; }
        public decimal DeclaredValue { get; }

        public RefrigeratedShipment(
            string trackingCode,
            string destinationCity,
            CargoWeight weight,
            double targetTemperatureCelsius,
            int transitHours,
            decimal declaredValue,
            decimal baseRatePerKg = 2.85m)
            : base(trackingCode, destinationCity, weight, ShipmentPriority.ColdChain, baseRatePerKg)
        {
            if (targetTemperatureCelsius < -30.0 || targetTemperatureCelsius > 15.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetTemperatureCelsius),
                    "Cold-chain target temperature must be between -30°C and 15°C.");
            }

            TargetTemperatureCelsius = targetTemperatureCelsius;
            TransitHours = transitHours > 0 ? transitHours : 1;
            DeclaredValue = declaredValue >= 0m ? decimal.Round(declaredValue, 2) : 0m;
        }

        public decimal CalculateInsurancePremium()
        {
            if (DeclaredValue <= 0m)
            {
                return 0m;
            }

            // Higher premium rate due to temperature-excursion spoilage risk
            return Math.Max(25.00m, decimal.Round(DeclaredValue * 0.018m, 2));
        }

        public override decimal CalculateFreightCost(SurchargePolicy customPolicy = null)
        {
            decimal hourlyCoolingRate = TargetTemperatureCelsius < 0.0 ? 4.50m : 2.75m;
            decimal coolingFee = TransitHours * hourlyCoolingRate;
            decimal baseFreight = (Weight.Kilograms * BaseRatePerKg) + coolingFee;

            return ApplyPolicy(baseFreight, customPolicy);
        }

        public override string GetManifestLine()
        {
            return $"{base.GetManifestLine()} | Temp: {TargetTemperatureCelsius:0.#}°C ({TransitHours}h), Ins: ${CalculateInsurancePremium():N2}";
        }
    }

    #endregion

    #region Extension Methods

    /// <summary>
    /// Extension methods for CargoWeight formatting and Shipment collection queries.
    /// </summary>
    public static class ShipmentExtensions
    {
        public static string ToDisplayWeight(this CargoWeight weight)
        {
            return weight.Kilograms >= 1000m
                ? $"{weight.Kilograms:N2} kg ({weight.MetricTons:N2} t)"
                : $"{weight.Kilograms:N2} kg";
        }

        public static CargoWeight TotalCargoWeight(this IEnumerable<Shipment> shipments)
        {
            if (shipments == null)
            {
                throw new ArgumentNullException(nameof(shipments));
            }

            CargoWeight total = CargoWeight.Zero;
            foreach (Shipment shipment in shipments)
            {
                total += shipment.Weight;
            }

            return total;
        }

        public static IEnumerable<Shipment> FilterByMinWeight(this IEnumerable<Shipment> shipments, CargoWeight minWeight)
        {
            if (shipments == null)
            {
                throw new ArgumentNullException(nameof(shipments));
            }

            return shipments.Where(s => s.Weight >= minWeight);
        }
    }

    #endregion

    #region Dispatch Manifest Coordinator

    /// <summary>
    /// Coordinates shipment registration, dispatch auditing, surcharge policies, and LINQ reporting.
    /// </summary>
    public class DispatchManifest
    {
        private readonly Dictionary<string, Shipment> shipments = new Dictionary<string, Shipment>(StringComparer.OrdinalIgnoreCase);
        private readonly Action<string> auditLogger;

        public DispatchManifest(Action<string> auditLogger = null)
        {
            this.auditLogger = auditLogger;
        }

        public IReadOnlyCollection<Shipment> AllShipments => shipments.Values;

        public void RegisterShipment(Shipment shipment)
        {
            if (shipment == null)
            {
                throw new ArgumentNullException(nameof(shipment));
            }

            if (!shipments.TryAdd(shipment.TrackingCode, shipment))
            {
                throw new InvalidOperationException($"Duplicate tracking code '{shipment.TrackingCode}' cannot be registered.");
            }

            auditLogger?.Invoke($"Registered [{shipment.TrackingCode}] -> {shipment.DestinationCity} ({shipment.Weight.ToDisplayWeight()})");
        }

        public void DispatchShipment(string trackingCode, string originHub, DateTime? timestamp = null)
        {
            if (!shipments.TryGetValue(trackingCode, out Shipment shipment))
            {
                throw new KeyNotFoundException($"Tracking code '{trackingCode}' was not found in the manifest.");
            }

            shipment.Dispatch(originHub, timestamp);
            auditLogger?.Invoke($"Dispatched [{shipment.TrackingCode}] from {originHub.ToUpperInvariant()}");
        }

        public decimal CalculateTotalFreight(SurchargePolicy policy = null)
        {
            return shipments.Values.Sum(s => s.CalculateFreightCost(policy));
        }

        public decimal CalculateTotalInsurancePremiums()
        {
            return shipments.Values
                .OfType<IInsuredCargo>()
                .Sum(insured => insured.CalculateInsurancePremium());
        }

        public IEnumerable<(ShipmentPriority Priority, int Count, CargoWeight TotalWeight, decimal StandardFreight)> SummarizeByPriority(
            SurchargePolicy policy = null)
        {
            return shipments.Values
                .GroupBy(s => s.Priority)
                .OrderByDescending(g => g.Key)
                .Select(g => (
                    Priority: g.Key,
                    Count: g.Count(),
                    TotalWeight: g.TotalCargoWeight(),
                    StandardFreight: g.Sum(s => s.CalculateFreightCost(policy))
                ));
        }
    }

    #endregion

    #region Solution Runner

    /// <summary>
    /// Demonstrates CargoWeight operator overloading, delegate surcharge policies, extension methods, and LINQ summaries.
    /// </summary>
    public static class Day20261127Part3Solution
    {
        public static void Run()
        {
            Console.WriteLine("==========================================================================");
            Console.WriteLine("  Daily C# Practice (2026-11-27 Part 3) - Freight & Logistics Dispatch");
            Console.WriteLine("==========================================================================\n");

            List<string> auditTrail = new List<string>();
            DispatchManifest manifest = new DispatchManifest(msg => auditTrail.Add(msg));

            // 1. Demonstrate CargoWeight value type & operator overloading
            CargoWeight palletA = new CargoWeight(420.50m);
            CargoWeight palletB = CargoWeight.FromTons(0.35m); // 350.00 kg
            CargoWeight combinedWeight = palletA + palletB;
            Console.WriteLine($"Weight Arithmetic: {palletA} + {palletB} = {combinedWeight.ToDisplayWeight()}");
            Console.WriteLine($"Weight Comparison: {palletA} > {palletB} is {palletA > palletB}\n");

            // 2. Register polymorphic shipment hierarchy
            GroundFreightShipment groundLocal = new GroundFreightShipment(
                "TRK-1001", "Chattogram", combinedWeight, distanceKm: 245, requiresLiftgate: true);

            GroundFreightShipment groundLongHaul = new GroundFreightShipment(
                "TRK-1002", "Sylhet", CargoWeight.FromTons(1.45m), distanceKm: 920, requiresLiftgate: false, priority: ShipmentPriority.Economy);

            AirCargoShipment airPharma = new AirCargoShipment(
                "AIR-2040", "Singapore", new CargoWeight(185.00m), declaredValue: 18500.00m, isHazardous: false);

            AirCargoShipment airHazmat = new AirCargoShipment(
                "AIR-2041", "Dubai", new CargoWeight(90.00m), declaredValue: 9200.00m, isHazardous: true);

            RefrigeratedShipment coldChain = new RefrigeratedShipment(
                "CLD-3090", "Rajshahi", new CargoWeight(610.00m), targetTemperatureCelsius: -18.0, transitHours: 14, declaredValue: 12000.00m);

            manifest.RegisterShipment(groundLocal);
            manifest.RegisterShipment(groundLongHaul);
            manifest.RegisterShipment(airPharma);
            manifest.RegisterShipment(airHazmat);
            manifest.RegisterShipment(coldChain);

            // 3. Dispatch and log checkpoints
            DateTime dispatchTime = new DateTime(2026, 11, 27, 9, 15, 0);
            manifest.DispatchShipment("TRK-1001", "DAC-HUB-1", dispatchTime);
            groundLocal.RecordCheckpoint("CGP-PORT", "Arrived at port terminal", dispatchTime.AddHours(5));

            manifest.DispatchShipment("AIR-2040", "DAC-CARGO", dispatchTime.AddMinutes(30));
            manifest.DispatchShipment("CLD-3090", "DAC-COLD", dispatchTime.AddMinutes(45));

            // 4. Print manifest lines
            Console.WriteLine("--- Active Dispatch Manifest ---");
            foreach (Shipment shipment in manifest.AllShipments)
            {
                Console.WriteLine("  " + shipment.GetManifestLine());
            }

            // 5. Compare standard pricing vs. peak-season SurchargePolicy delegate
            SurchargePolicy peakSeasonPolicy = (shipment, baseFreight) =>
            {
                // 8% fuel surcharge across all shipments + $25 priority handling for Express/ColdChain
                decimal fuel = baseFreight * 0.08m;
                decimal priorityHandling = shipment.Priority >= ShipmentPriority.Express ? 25.00m : 0m;
                return fuel + priorityHandling;
            };

            decimal standardTotal = manifest.CalculateTotalFreight();
            decimal peakTotal = manifest.CalculateTotalFreight(peakSeasonPolicy);
            decimal totalInsurance = manifest.CalculateTotalInsurancePremiums();

            Console.WriteLine("\n--- Pricing Policy Comparison ---");
            Console.WriteLine($"  Standard Freight Total   : ${standardTotal,9:N2}");
            Console.WriteLine($"  Peak-Season Policy Total : ${peakTotal,9:N2}");
            Console.WriteLine($"  Total Insured Premiums   : ${totalInsurance,9:N2}");

            // 6. Extension method filtering + LINQ priority grouping
            CargoWeight heavyThreshold = new CargoWeight(500.00m);
            List<Shipment> heavyShipments = manifest.AllShipments.FilterByMinWeight(heavyThreshold).ToList();

            Console.WriteLine($"\n--- Heavy Cargo (>= {heavyThreshold.ToDisplayWeight()}) ---");
            foreach (Shipment heavy in heavyShipments)
            {
                Console.WriteLine($"  [{heavy.TrackingCode}] {heavy.DestinationCity,-12} | {heavy.Weight.ToDisplayWeight()}");
            }
            Console.WriteLine($"  Combined Heavy Weight: {heavyShipments.TotalCargoWeight().ToDisplayWeight()}");

            Console.WriteLine("\n--- Summary by Priority ---");
            foreach (var group in manifest.SummarizeByPriority(peakSeasonPolicy))
            {
                Console.WriteLine($"  {group.Priority,-10} | Shipments: {group.Count} | Weight: {group.TotalWeight.ToDisplayWeight(),-22} | Peak Freight: ${group.StandardFreight,8:N2}");
            }

            Console.WriteLine($"\nAudit Events Recorded: {auditTrail.Count}");
            Console.WriteLine("==========================================================================");
        }
    }

    #endregion
}
