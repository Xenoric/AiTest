using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathBerserker2d
{
    public enum ResultType { NoOverlap, Clipped };
    public enum BoolOpType { INTERSECTION, UNION, DIFFERENCE };

    interface IClipper
    {
        ResultType Compute(Polygon sp, Polygon cp, BoolOpType op, out List<Polygon> result, bool includeOpenPolygons = false);
    }
}
