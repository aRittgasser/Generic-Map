using System;

namespace WpfTest.Services
{
    public class SomeService : ISomeService
    {
        public int GetRandomInt()
        {
            Random r = new Random();
            return r.Next();
        }
    }
}
