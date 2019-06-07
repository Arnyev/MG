using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MG
{
    public struct EdgeFuncs
    {
        public Func<float, Vector4> ValueA;
        public Func<float, Vector4> ValueB;
        public Func<float, Vector4> DerivativeA;
        public Func<float, Vector4> DerivativeB;
        public Func<Vector4> DuDvA;
        public Func<Vector4> DuDvB;
        public Func<Vector4> DuDvC;
        public Func<Vector4> DerivativeMiddle;
    }

    public class SimpleBezier
    {
        public Vector4 P1;
        public Vector4 P2;
        public Vector4 P3;
        public Vector4 P4;

        public Vector4 GetPoint(float t)
        {
            return P1 * (1 - t) * (1 - t) * (1 - t) + P2 * 3 * t * (1 - t) * (1 - t) +
                   P3 * 3 * (1 - t) * t * t + P4 * t * t * t;
        }

        public Vector4 DerivativeAt0() => (P2 - P1) * 3;

        public Vector4 DerivativeAt1() => (P3 - P4) * 3;
    }

    public class GregoryPatchContainer
    {
        private readonly BasicSurface _s1;
        private readonly BasicSurface _s2;
        private readonly BasicSurface _s3;
        private readonly UVDirection _d1;
        private readonly UVDirection _d2;
        private readonly UVDirection _d3;

        public EdgeFuncs EdgeFuncs1;
        public EdgeFuncs EdgeFuncs2;
        public EdgeFuncs EdgeFuncs3;

        public SimpleBezier SB1;
        public SimpleBezier SB2;
        public SimpleBezier SB3;

        public GregoryPatch patch1;
        public GregoryPatch patch2;
        public GregoryPatch patch3;

        public List<IDrawableObject> Patches
        {
            get
            {
                UpdateBeziers();
                return new List<IDrawableObject> { patch1 ,patch2, patch3};
            }
        }

        public int DivisionsU
        {
            get => patch1.DivisionsU;
            set { Patches.Cast<GregoryPatch>().ToList().ForEach(x => x.DivisionsU = value); }
        }

        public int DivisionsV
        {
            get => patch1.DivisionsV;
            set { Patches.Cast<GregoryPatch>().ToList().ForEach(x => x.DivisionsV = value); }
        }

        public GregoryPatchContainer(BasicSurface s1, BasicSurface s2, BasicSurface s3, UVDirection d1, UVDirection d2, UVDirection d3)
        {
            _s1 = s1;
            _s2 = s2;
            _s3 = s3;
            _d1 = d1;
            _d2 = d2;
            _d3 = d3;

            CreateEdgeFuncs();

            SB1 = new SimpleBezier();
            SB2 = new SimpleBezier();
            SB3 = new SimpleBezier();

            Vector4 Zerof(float t) => new Vector4();
            Vector4 Zeron() => new Vector4();

            patch1 = new GregoryPatch(
                valueFuncU0: EdgeFuncs1.ValueA,
                valueFuncU1: SB3.GetPoint,
                valueFuncV0: EdgeFuncs3.ValueA,
                valueFuncV1: SB1.GetPoint,
                derivativeFuncU0: EdgeFuncs1.DerivativeA,
                derivativeFuncU1: Zerof,
                derivativeFuncV0: EdgeFuncs3.DerivativeA,
                derivativeFuncV1: Zerof,
                duv00: EdgeFuncs1.DuDvA,
                dvu00: EdgeFuncs3.DuDvA,
                duv01: EdgeFuncs3.DuDvB,
                dvu01: EdgeFuncs3.DuDvB,
                duv10: EdgeFuncs1.DuDvB,
                dvu10: EdgeFuncs1.DuDvB,
                duv11: Zeron,
                dvu11: Zeron
            );

            patch2 = new GregoryPatch(
                valueFuncU0: EdgeFuncs1.ValueB,
                valueFuncU1: SB2.GetPoint,
                valueFuncV0: SB1.GetPoint,
                valueFuncV1: EdgeFuncs2.ValueA,
                derivativeFuncU0: EdgeFuncs1.DerivativeB,
                derivativeFuncU1: Zerof,
                derivativeFuncV0: Zerof,
                derivativeFuncV1: EdgeFuncs2.DerivativeA,
                duv00: EdgeFuncs1.DuDvB,
                dvu00: EdgeFuncs1.DuDvB,
                duv01: EdgeFuncs1.DuDvC,
                dvu01: EdgeFuncs2.DuDvA,
                duv10: EdgeFuncs1.DuDvB,
                dvu10: EdgeFuncs1.DuDvB,
                duv11: Zeron,
                dvu11: Zeron
            );

            patch3 = new GregoryPatch(
                valueFuncU0: SB3.GetPoint,
                valueFuncU1: EdgeFuncs2.ValueB,
                valueFuncV0: EdgeFuncs3.ValueB,
                valueFuncV1: SB2.GetPoint,
                derivativeFuncU0: Zerof,
                derivativeFuncU1: EdgeFuncs2.DerivativeB,
                derivativeFuncV0: EdgeFuncs3.DerivativeB,
                derivativeFuncV1: Zerof,
                duv00: EdgeFuncs3.DuDvB,
                dvu00: EdgeFuncs3.DuDvB,
                duv01: Zeron,
                dvu01: Zeron,
                duv10: EdgeFuncs2.DuDvC,
                dvu10: EdgeFuncs3.DuDvC,
                duv11: EdgeFuncs2.DuDvB,
                dvu11: EdgeFuncs2.DuDvB
            );
        }

        private Vector4 P1DU1(float t)
        {
            var p1 = EdgeFuncs3.DerivativeMiddle();
            var p2 = SB1.DerivativeAt0();
            return (1 - t) * p1 + t * p2;
        }

        private Vector4 P1DV1(float t)
        {
            var p1 = Vector4.Normalize(EdgeFuncs1.DerivativeMiddle()/2);
            var p2 = Vector4.Normalize(-SB3.DerivativeAt1());
            return (1 - t) * p1 + t * p2;
        }

        public void UpdateBeziers()
        {
            var px1 = EdgeFuncs1.ValueA(0);
            var px2 = EdgeFuncs2.ValueA(0);
            var px3 = EdgeFuncs3.ValueA(0);
            var px4 = EdgeFuncs1.ValueB(1);
            var px5 = EdgeFuncs2.ValueB(0);

            var p31 = EdgeFuncs1.ValueA(1);
            var p32 = EdgeFuncs2.ValueA(1);
            var p33 = EdgeFuncs3.ValueA(1);

            var d1 = EdgeFuncs1.DerivativeA(1) / 3;
            var d2 = EdgeFuncs2.DerivativeA(1) / 3;
            var d3 = EdgeFuncs3.DerivativeA(1) / 3;

            var f1 = IsAt0(_d1) ? -1 : 1;
            var f2 = IsAt0(_d2) ? -1 : 1;
            var f3 = IsAt0(_d3) ? -1 : 1;

            var p21 = p31 + f1 * EdgeFuncs1.DerivativeA(1) / 6;
            var p22 = p32 + f2 * EdgeFuncs2.DerivativeA(1) / 6;
            var p23 = p33 + f3 * EdgeFuncs3.DerivativeA(1) / 6;

            var q1 = (3 * p21 - p31) / 2;
            var q2 = (3 * p22 - p32) / 2;
            var q3 = (3 * p23 - p33) / 2;

            var p = (q1 + q2 + q3) / 3;

            var p11 = (2 * q1 + p) / 3;
            var p12 = (2 * q2 + p) / 3;
            var p13 = (2 * q3 + p) / 3;

            SB1.P1 = p31;
            SB1.P2 = p21;
            SB1.P3 = p11;
            SB1.P4 = p;

            SB2.P1 = p;
            SB2.P2 = p12;
            SB2.P3 = p22;
            SB2.P4 = p32;

            SB3.P1 = p33;
            SB3.P2 = p23;
            SB3.P3 = p13;
            SB3.P4 = p;
        }

        private void CreateEdgeFuncs()
        {
            var v1a = _s1.GetValueFunc(_d1, true);
            var v1b = _s1.GetValueFunc(_d1, false);
            var d1a = _s1.GetDerivativeFunc(_d1, true);
            var d1b = _s1.GetDerivativeFunc(_d1, false);

            var tdu1a = _s1.GetSecondDerivativeFuncs(_d1, true);
            var tdu1b = _s1.GetSecondDerivativeFuncs(_d1, false);

            EdgeFuncs1 = new EdgeFuncs
            {
                ValueA = v1a,
                ValueB = v1b,
                DerivativeA = d1a,
                DerivativeB = d1b,
                DuDvA = tdu1a.Item1,
                DuDvB = tdu1a.Item2,
                DuDvC = tdu1b.Item2,
                DerivativeMiddle = _s1.DerivativeMiddle(_d1)
            };

            var d2o = OppositeDirection(_d2);
            var v2a = _s2.GetValueFunc(_d2, true);
            var v2b = _s2.GetValueFunc(d2o, true);
            var d2a = _s2.GetDerivativeFunc(_d2, true);
            var d2b = _s2.GetDerivativeFunc(d2o, true);

            var tdu2a = _s2.GetSecondDerivativeFuncs(_d2, true);
            var tdu2b = _s2.GetSecondDerivativeFuncs(_d2, false);

            EdgeFuncs2 = new EdgeFuncs
            {
                ValueA = v2a,
                ValueB = v2b,
                DerivativeA = d2a,
                DerivativeB = d2b,
                DuDvA = tdu2a.Item1,
                DuDvB = tdu2a.Item2,
                DuDvC = tdu2b.Item2,
                DerivativeMiddle = _s2.DerivativeMiddle(_d2)
            };

            var v3a = _s3.GetValueFunc(_d3, true);
            var v3b = _s3.GetValueFunc(_d3, false);
            var d3a = _s3.GetDerivativeFunc(_d3, true);
            var d3b = _s3.GetDerivativeFunc(_d3, false);

            var tdu3a = _s3.GetSecondDerivativeFuncs(_d3, true);
            var tdu3b = _s3.GetSecondDerivativeFuncs(_d3, false);

            EdgeFuncs3 = new EdgeFuncs
            {
                ValueA = v3a,
                ValueB = v3b,
                DerivativeA = d3a,
                DerivativeB = d3b,
                DuDvA = tdu3a.Item1,
                DuDvB = tdu3a.Item2,
                DuDvC = tdu3b.Item2,
                DerivativeMiddle = _s3.DerivativeMiddle(_d3),
            };
        }

        public static UVDirection OppositeDirection(UVDirection d)
        {
            switch (d)
            {
                case UVDirection.U0V01:
                    return UVDirection.U0V10;
                case UVDirection.U0V10:
                    return UVDirection.U0V01;
                case UVDirection.U1V01:
                    return UVDirection.U1V10;
                case UVDirection.U1V10:
                    return UVDirection.U1V01;
                case UVDirection.V0U01:
                    return UVDirection.V0U10;
                case UVDirection.V0U10:
                    return UVDirection.V0U01;
                case UVDirection.V1U01:
                    return UVDirection.V1U10;
                case UVDirection.V1U10:
                    return UVDirection.V1U01;
                default:
                    throw new ArgumentOutOfRangeException(nameof(d), d, null);
            }
        }

        public static bool IsAt0(UVDirection d)
        {
            switch (d)
            {
                case UVDirection.U0V01:
                case UVDirection.U0V10:
                case UVDirection.V0U01:
                case UVDirection.V0U10:
                    return true;
                case UVDirection.V1U01:
                case UVDirection.V1U10:
                case UVDirection.U1V01:
                case UVDirection.U1V10:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(d), d, null);
            }
        }
    }
}
