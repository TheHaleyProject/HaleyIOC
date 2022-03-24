using System;
using System.Collections.Generic;
using System.Text;
using Haley.Models;

namespace Haley.Utils
{
    public class RegisterLoadComparer : IEqualityComparer<RegisterLoad>
    {
        public bool Equals(RegisterLoad x, RegisterLoad y)
        {
            
            if (x == null || y == null) return false;
            if (x.ContractType == null || y.ContractType == null) return false;
            if (x.ContractType != y.ContractType || x.PriorityKey != y.PriorityKey) return false;

            return true;
        }

        public int GetHashCode(RegisterLoad obj)
        {
            return (obj.PriorityKey + obj.ContractType?.ToString()).GetHashCode();
        }
    }
}
