using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppInheritanceT
{
    internal abstract class Parent
    {
        internal Parent() //: this("Hello")
        {
            Console.WriteLine("Parent D");
        }

        internal Parent(int h) //: this()
        {
            Console.WriteLine("Parent NDC-I: " + h);
        }

        internal Parent(string h)
        {
            Console.WriteLine("Parent NDC-S: " + h);
        }

        internal void MethodA()
        {
            Console.WriteLine("Parent MethodA");
        }

        internal virtual void MethodB()
        {
            Console.WriteLine("Parent MethodB");
        }

        internal virtual void MethodC()
        {
            Console.WriteLine("Parent MethodC");
        }

        internal virtual void MethodD()
        {
            Console.WriteLine("Parent MethodD");
        }

        internal abstract void MethodE();
        internal abstract void MethodF(int r);
    }
}
