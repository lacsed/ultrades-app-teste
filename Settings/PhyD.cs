using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAVL.Settings
{
    public class PhyD
    {
        public double stopCondition = 0.005f;
        public double attenuation = 0.1f;
        public double repulsion = 1000.0f;
        public double elastic = 0.005f;

        public PhyD()
        {
            stopCondition = 0.005f;
        }
    }
}
