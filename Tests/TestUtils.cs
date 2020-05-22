using System;
using System.Collections.Generic;
using System.Text;

namespace Tests
{
    public class TestUtils
    {

        public static string GetRandomString(int length)
        {
            if(length < 25)
                return Guid.NewGuid().ToString().Substring(0, length);
            else
            {
                string builtUp = "";
                while(builtUp.Length < length)
                {
                    builtUp += Guid.NewGuid().ToString();
                }
                return builtUp.Substring(0, length);
            }
        }

    }
}
