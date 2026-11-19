using System;
using System.Collections.Generic;

namespace PracticeProblems
{
    /// <summary>
    /// Daily Practice Problem - 2026-11-19
    /// Topic: OOP Inheritance, Polymorphism, Abstraction, and Interface Implementation
    /// 
    /// Problem Statement:
    /// Design a clean Order Processing System for an online store:
    /// 1. Create an abstract base class 'Order' containing common fields (OrderId, CustomerName, BaseAmount),
    ///    constructors with chaining, an abstract method 'CalculateTotal()', and a virtual method 'DisplayOrderSummary()'.
    /// 2. Implement an interface 'ITrackable' with a property 'TrackingNumber' and method 'GetTrackingStatus()'.
    /// 3. Create derived classes:
    ///    - 'StandardOrder': Adds fixed standard shipping fee ($10.00).
    ///    - 'PremiumOrder' (implements ITrackable): Applies a VIP percentage discount and provides free shipping for orders over $100.
    ///    - 'ExpressOrder' (implements ITrackable): Adds priority expedited shipping ($25.00) with guaranteed delivery date tracking.
    /// 4. Demonstrate polymorphic behavior by iterating over a collection of different order types and printing summaries.
    /// </summary>

    #region Interfaces

    /// <summary>
    /// Contract for orders that support carrier tracking.
    /// </summary>
    public interface ITrackable
    {
        string TrackingNumber { get; }
        string GetTrackingStatus();
    }

    #endregion

    #region Base Order Class

    /// <summary>
    /// Abstract base class representing a generic order.
    /// Demonstrates encapsulation, protected member access, and constructor chaining.
    /// </summary>
    public abstract class Order
    {
        private string orderId;
        private string customerName;
        private decimal baseAmount;

        public string OrderId
        {
            get => orderId;
            protected set => orderId = string.IsNullOrWhiteSpace(value) ? "UNKNOWN" : value;
        }

        public string CustomerName
        {
            get => customerName;
            set => customerName = string.IsNullOrWhiteSpace(value) ? "Valued Customer" : value;
        }

        public decimal BaseAmount
        {
            get => baseAmount;
            set => baseAmount = value >= 0 ? value : 0m;
        }

        /// <summary>
        /// Base constructor with validation.
        /// </summary>
        protected Order(string orderId, string customerName, decimal baseAmount)
        {
            OrderId = orderId;
            CustomerName = customerName;
            BaseAmount = baseAmount;
        }

        /// <summary>
        /// Abstract method to calculate final payable order total, overridden in subclasses.
        /// </summary>
        public abstract decimal CalculateTotal();

        /// <summary>
        /// Virtual method providing standard formatted summary; can be extended by derived classes.
        /// </summary>
        public virtual void DisplayOrderSummary()
        {
            Console.WriteLine($"[Order ID: {OrderId}]");
            Console.WriteLine($"Customer:    {CustomerName}");
            Console.WriteLine($"Base Amount: ${BaseAmount:F2}");
            Console.WriteLine($"Final Total: ${CalculateTotal():F2}");
        }
    }

    #endregion

    #region Derived Order Classes

    /// <summary>
    /// Standard order with fixed standard shipping charges.
    /// </summary>
    public class StandardOrder : Order
    {
        public const decimal StandardShippingFee = 10.00m;

        public StandardOrder(string orderId, string customerName, decimal baseAmount)
            : base(orderId, customerName, baseAmount)
        {
        }

        public override decimal CalculateTotal()
        {
            return BaseAmount + StandardShippingFee;
        }

        public override void DisplayOrderSummary()
        {
            base.DisplayOrderSummary();
            Console.WriteLine($"Shipping:    Standard (${StandardShippingFee:F2})");
        }
    }

    /// <summary>
    /// Premium member order providing tiered VIP discount and free shipping for orders over $100.
    /// Implements ITrackable.
    /// </summary>
    public class PremiumOrder : Order, ITrackable
    {
        public decimal DiscountPercentage { get; set; }
        public string TrackingNumber { get; private set; }

        public PremiumOrder(string orderId, string customerName, decimal baseAmount, decimal discountPercentage, string trackingNumber)
            : base(orderId, customerName, baseAmount)
        {
            DiscountPercentage = Math.Clamp(discountPercentage, 0m, 50m);
            TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? "TRK-PENDING" : trackingNumber;
        }

        public override decimal CalculateTotal()
        {
            decimal discountedAmount = BaseAmount * (1 - (DiscountPercentage / 100m));
            // Free shipping if discounted total >= $100, otherwise $5 nominal fee
            decimal shippingFee = discountedAmount >= 100.00m ? 0.00m : 5.00m;
            return discountedAmount + shippingFee;
        }

        public string GetTrackingStatus()
        {
            return $"VIP Expedited Delivery: In Transit via Carrier (Track: {TrackingNumber})";
        }

        public override void DisplayOrderSummary()
        {
            base.DisplayOrderSummary();
            Console.WriteLine($"VIP Discount: {DiscountPercentage}%");
            Console.WriteLine($"Tracking:     {GetTrackingStatus()}");
        }
    }

    /// <summary>
    /// Express order with expedited shipping and urgent delivery guarantee.
    /// Implements ITrackable.
    /// </summary>
    public class ExpressOrder : Order, ITrackable
    {
        public const decimal ExpressShippingFee = 25.00m;
        public string TrackingNumber { get; private set; }
        public int EstimatedDeliveryDays { get; set; }

        public ExpressOrder(string orderId, string customerName, decimal baseAmount, string trackingNumber, int estimatedDays = 1)
            : base(orderId, customerName, baseAmount)
        {
            TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? "EXP-PENDING" : trackingNumber;
            EstimatedDeliveryDays = estimatedDays > 0 ? estimatedDays : 1;
        }

        public override decimal CalculateTotal()
        {
            return BaseAmount + ExpressShippingFee;
        }

        public string GetTrackingStatus()
        {
            return $"Guaranteed Next-Step Delivery in {EstimatedDeliveryDays} business day(s) (Track: {TrackingNumber})";
        }

        public override void DisplayOrderSummary()
        {
            base.DisplayOrderSummary();
            Console.WriteLine($"Shipping:    Express (${ExpressShippingFee:F2})");
            Console.WriteLine($"Tracking:    {GetTrackingStatus()}");
        }
    }

    #endregion

    #region Problem Solution & Verification Runner

    /// <summary>
    /// Driver class to verify problem solution and demonstrate polymorphism.
    /// </summary>
    public static class Day20261119Solution
    {
        /// <summary>
        /// Demonstrates problem solution with polymorphic collection traversal and type checking.
        /// </summary>
        public static void Run()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine(" Daily Practice Problem: 2026-11-19 Solution");
            Console.WriteLine("=================================================\n");

            var orders = new List<Order>
            {
                new StandardOrder("ORD-1001", "Alice Smith", 85.00m),
                new PremiumOrder("ORD-1002", "Bob Johnson", 150.00m, 15m, "TRK-VIP-99812"),
                new ExpressOrder("ORD-1003", "Charlie Brown", 65.50m, "TRK-EXP-44120", 1)
            };

            decimal totalRevenue = 0m;

            foreach (var order in orders)
            {
                order.DisplayOrderSummary();
                totalRevenue += order.CalculateTotal();

                if (order is ITrackable trackable)
                {
                    Console.WriteLine($"[Interface Check] Tracking confirmed: {trackable.TrackingNumber}");
                }

                Console.WriteLine(new string('-', 45));
            }

            Console.WriteLine($"\nTotal Processed Revenue: ${totalRevenue:F2}");
            Console.WriteLine("=================================================");
        }
    }

    #endregion
}
