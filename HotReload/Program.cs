using Urho;
using System;

namespace HotReload
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new HotReload().Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}

