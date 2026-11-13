using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppInheritanceT
{
    internal class Employee : Person
    {
        private double salary;

        internal double Salary
        {
            get { return this.salary; }
            set
            {
                if (value >= 0.0)
                    this.salary = value;
                else
                    this.salary = -2;
            }
        }

        internal Employee(uint id, string name, string bloodGroup, AddressFormat address, double salary) : base(id, name, bloodGroup, address)
        {
            this.Salary = salary;
        }

        public override void PrintInfo()
        {
            base.PrintInfo();
            Console.WriteLine($"Salary: {this.Salary}");
        }
    }
}
