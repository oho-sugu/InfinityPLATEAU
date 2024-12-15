﻿using System;


namespace InfinityPLATEAU
{
    /// <summary>
    /// 座標の変換用クラス。なるべくシンプルに必要な変換だけ。
    /// 測地系はWGS84を使用。
    /// 計算方法は「世界測地系と座標変換　飛田幹夫　日本測量協会」を参考にした。
    /// </summary>
    public class CoordConv
    {
        private const double a = 6378137.0;
        private const double f = 1.0 / 298.257223563;
        private const double e2 = f * (2.0 - f);

        /// <summary>
        /// 緯度経度（WGS84）から直交座標（ECEF）に変換するメソッド。
        /// </summary>
        /// <param name="b">緯度</param>
        /// <param name="l">経度</param>
        /// <param name="h">楕円体高</param>
        /// <returns>直交座標のXYZ。+Zが北極、+Xが子午線、+Yが東経方向</returns>
        public static (double x, double y, double z) BLH2XYZ(double b, double l, double h)
        {
            b = Math.PI * b / 180.0;
            l = Math.PI * l / 180.0;

            double N = a / Math.Sqrt(1.0 - e2 * Math.Pow(Math.Sin(b), 2.0));

            return (
                (N + h) * Math.Cos(b) * Math.Cos(l),
                (N + h) * Math.Cos(b) * Math.Sin(l),
                (N * (1.0 - e2) + h) * Math.Sin(b)
            );
        }

        /// <summary>
        /// 直交座標（ECEF）から緯度経度（WGS84）に変換するメソッド。
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <param name="z">Z</param>
        /// <returns>緯度経度。b緯度、l経度、h楕円体高</returns>
        public static (double b, double l, double h) XYZ2BLH(double x, double y, double z)
        {
            double p = Math.Sqrt(x * x + y * y);
            double r = Math.Sqrt(p * p + z * z);
            double mu = Math.Atan(z / p * ((1.0 - f) + e2 * a / r));

            double B = Math.Atan((z * (1.0 - f) + e2 * a * Math.Pow(Math.Sin(mu), 3)) / ((1.0 - f) * (p - e2 * a * Math.Pow(Math.Cos(mu), 3))));
            return (
                180.0 * B / Math.PI,
                180.0 * Math.Atan2(y, x) / Math.PI,
                p * Math.Cos(B) + z * Math.Sin(B) - a * Math.Sqrt(1.0 - e2 * Math.Pow(Math.Sin(B), 2))
            );
        }

        public static (double, double, double) CalcOffset(double lat, double lon, double clat, double clon)
        {
            (var cx, var cy, var cz) = BLH2XYZ(clat, clon, 0);
            (var x, var y, var z) = BLH2XYZ(lat, lon, 0);

            (x, y, z) = (x - cx, y - cy, z - cz);

            double s = (-clon) * Math.PI / 180.0;
            double rx = x * Math.Cos(s) - y * Math.Sin(s);
            double ry = x * Math.Sin(s) + y * Math.Cos(s);

            s = (-clat) * Math.PI / 180.0;
            double rxx = rx * Math.Cos(s) - z * Math.Sin(s);
            double rz = rx * Math.Sin(s) + z * Math.Cos(s);

            return (rxx, ry, rz);
        }

    }
}
