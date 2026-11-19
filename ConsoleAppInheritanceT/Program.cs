using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppInheritanceT
{
    class Program
    {
        static void Main(string[] args)
        {
            //Person p1 = new Person(100, "Bruce", "B+", new AddressFormat(42, 192, 3423, "Sylhet"));
            //p1.PrintPersonInfo();
            //Person s1 = new Student(200, "Clerk", "O+", new AddressFormat(12, 43, 2939, "Khulna"), 3.45);
            //s1.PrintPersonInfo();
            //Person e1 = new Employee(300, "Diana", "AB+", new AddressFormat(20, 81, 3928, "Feni"), 15000);
            //e1.PrintPersonInfo();

            Person[] people = new Person[3];
            people[0] = new Student(100, "Bruce", "B+", new AddressFormat(42, 192, 3423, "Sylhet"), 3.65);
            people[1] = new Student(200, "Clerk", "O+", new AddressFormat(12, 43, 2939, "Khulna"), 3.45);
            people[2] = new Employee(300, "Diana", "AB+", new AddressFormat(20, 81, 3928, "Feni"), 15000);

            foreach (Person p in people)
            {
                p.PrintInfo();
                Console.WriteLine();
            }

            //CTS
            Object o1 = 12;//boxing
            Object o2 = 3.19;
            Object o3 = "Hello";
            Object o4 = new Child();
            int h = (int)o1;//unboxing
            Console.WriteLine(o1.GetType());
            Console.WriteLine(o2.GetType());
            Console.WriteLine(o3.GetType());
            Console.WriteLine(o4.GetType());

            //anonymous type, dynamic type
            //var a = 98;
            //var b = 34.65;
            //var c = "test";
            //var d = new Child();
            //Console.WriteLine(a.GetType());
            //Console.WriteLine(b.GetType());
            //Console.WriteLine(c.GetType());
            //Console.WriteLine(d.GetType());

            //dynamic m = 13;
            //dynamic n = new Parent();
            //n.FakeMethod();

            //Nullable type
            Nullable<int> r1 = null;
            int? r2 = null;
            int r3 = r2 ?? 50;
            if (r2.HasValue) { }
            //tuple value, TryParse()
            Int32.TryParse("32", out r3);

            Parent p1;// = new Parent();
            //Parent p2 = new Parent(23);
            //Child c1 = new Child();
            ////Child c2 = new Child(10);
            Parent pp = new Child();
            //Console.WriteLine();

            //p1.MethodA();
            //c1.MethodA();
            //p1.MethodB();
            //c1.MethodB();
            //pp.MethodA();
            //pp.MethodB();
            //pp.MethodC();
            //pp.MethodD();
        }
    }
}
