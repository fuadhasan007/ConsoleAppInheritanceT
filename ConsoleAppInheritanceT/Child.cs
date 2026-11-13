using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppInheritanceT
{
    internal class Child : Parent
    {
        internal Child() //: base(9)
        {
            Console.WriteLine("Child D");
        }

        internal Child(int q) : base(q)//: this("Test")
        {
            Console.WriteLine("Child NDC-I: " + q);
        }

        internal Child(string w) : this()
        {
            Console.WriteLine("Child NDC-S: " + w);
        }

        internal new void MethodA()
        {
            Console.WriteLine("Child MethodA");
        }

        internal override void MethodB()
        {
            Console.WriteLine("Child MethodB");
        }

        internal override void MethodC()
        {
            Console.WriteLine("Child MethodC");
        }

        internal new void MethodD()
        {
            Console.WriteLine("Child MethodD");
        }

        internal override void MethodE()
        {
            Console.WriteLine("Child MethodE");
        }

        internal override void MethodF(int w)
        {
            Console.WriteLine("Child MethodF");
        }
    }
}
