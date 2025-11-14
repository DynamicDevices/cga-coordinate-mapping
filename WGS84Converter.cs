using System;
using System.Collections.Generic;
using System.Numerics;

/*             geodesy routines in JavaScript
                 James R. Clynch NPS / 2003
               
          Done for support of web education pages

    Changed to C# in Unity and added to by Jen 
Laing / 2020
    Created for use in Simulations Only.
    DO NOT USE FOR NAVIGATION IN REAL LIFE AS NOT 100% ACCURATE, ERRORS MAY CUMULATE AND DEATH COULD OCCUR.
*/


public class WGS84Converter
{
    //Jen20
    //Set this to whatever your north is in terms of (x, z) as a unit vector. eg (1, 0) is +x = north 
    //N.b. +y assumed to be altitude vector (ie perpendicular to and away from the Earth) 
    private static Vector2 unitVectorNorth = new Vector2(0, -1);
    private static float[,] rotAdjMatrix_ACW; //rotates a vector from if n = +x to wherever north is
    private static float[,] rotAdjMatrix_CW; //rotates a vector from wherever north is to if north was +x

    private static double EARTH_A;
    private static double EARTH_B;
    private static double EARTH_F;
    private static double EARTH_Ecc;
    private static double EARTH_Esq;

    #region WGS84 Constant Setting
    // =======================================================================

    private static void geodGBL()
    //test and ensure geodesy globals loaded
    {

        double tstglobal = EARTH_A;

        if (tstglobal == 0)
        {
            wgs84();            
        }
    }

    //========================================================================
    private static void SetAdjMatrix()
    //Jen20 - Sets adj for north not always being +x
    {
        //nb theta here increases acw from +x - ie opposite direction to bearings and unity because it must to be able to use this transformation matrix method.
        //=> must convert back and forth to bearings/unity angles if this theta is ever accessed from the matrix.
        float cos_theta = unitVectorNorth.X;
        float sin_theta = unitVectorNorth.Y;
        rotAdjMatrix_ACW = new float[2, 2] { { cos_theta, -sin_theta }, { sin_theta, cos_theta } };
        rotAdjMatrix_CW = new float[2, 2] { { cos_theta, sin_theta }, { -sin_theta, cos_theta } };
    }

    // =======================================================================

    private static void wgs84()
    /*        WGS84 Earth Constants
                 --  Leaves Globals   EARTH_A   EARTH_B   EARTH_F  EARTH_Ecc    EARTH_Esq
    */
    {
        double wgs84a, wgs84b, wgs84f;

        wgs84a = 6378.137d;
        wgs84f = 1.0d / 298.257223563d;
        wgs84b = wgs84a * (1.0d - wgs84f);

        earthcon(wgs84a, wgs84b);
    }

    // =======================================================================

    private static void earthcon(double a, double b)
    /* Sets Earth Constants as globals
        --  input a,b
        --  Leaves Globals EARTH_A   EARTH_B   EARTH_F  EARTH_Ecc   EART_Esq
    */
    {
        double f, ecc, eccsq;

        f = 1 - b / a;
        eccsq = 1 - b * b / (a * a);
        ecc = Math.Sqrt(eccsq);

        EARTH_A = a;
        EARTH_B = b;
        EARTH_F = f;
        EARTH_Ecc = ecc;
        EARTH_Esq = eccsq;
    }

    #endregion

    #region Methods
    // =======================================================================

    private static double[] radcur(double lat)
    /* compute the radii at the geodetic latitude lat (in degrees)
     
         input:
                   lat       geodetic latitude in degrees
         output:   
                   rrnrm     an array 3 long
                             r,  rn,  rm   in km
    */
    {
        double[] rrnrm = new double[3];

        double dtr = Math.PI / 180.0d;
        double a, b;
        double asq, bsq, eccsq, ecc, clat, slat;
        double dsq, d, rn, rm, rho, rsq, r, z;

        geodGBL();

        a = EARTH_A;
        b = EARTH_B;

        asq = a * a;
        bsq = b * b;
        eccsq = 1 - bsq / asq;
        ecc = Math.Sqrt(eccsq);

        clat = Math.Cos(dtr * lat);
        slat = Math.Sin(dtr * lat);

        dsq = 1.0d - eccsq * slat * slat;
        d = Math.Sqrt(dsq);

        rn = a / d;
        rm = rn * (1.0d - eccsq) / dsq;

        rho = rn * clat;
        z = (1.0d - eccsq) * rn * slat;
        rsq = rho * rho + z * z;
        r = Math.Sqrt(rsq);

        rrnrm[0] = r;
        rrnrm[1] = rn;
        rrnrm[2] = rm;

        return (rrnrm);

    }

    // =======================================================================

    private static double rearth(double lat)
    // physical radius of earth from geodetic latitude
    {

        double[] rrnrm = new double[3];
        rrnrm = radcur(lat);
        double r = rrnrm[0];

        return (r);
    }

    // =======================================================================

    private static double gc2gd(double flatgc, double altkm)
    /* geocentric latitude to geodetic latitude

         Input:
                   flatgc    geocentric latitude deg.
                   altkm     altitide in km
         ouput:
                   flatgd    geodetic latitude in deg

    */
    {

        double dtr = Math.PI / 180.0f;
        double rtd = 1 / dtr;

        double flatgd;
        double[] rrnrm = new double[3];
        double re, rn, ecc, esq;
        double slat, clat, tlat;
        double altnow, ratio;

        geodGBL();

        ecc = EARTH_Ecc;
        esq = ecc * ecc;

        // approximation by stages
        // 1st use gc-lat as if is gd, then correct alt dependence

        altnow = altkm;

        rrnrm = radcur(flatgc);
        rn = rrnrm[1];

        ratio = 1 - esq * rn / (rn + altnow);

        tlat = Math.Tan(dtr * flatgc) / ratio;
        flatgd = rtd * Math.Atan(tlat);

        // now use this approximation for gd-lat to get rn etc.

        rrnrm = radcur(flatgd);
        rn = rrnrm[1];

        ratio = 1 - esq * rn / (rn + altnow);
        tlat = Math.Tan(dtr * flatgc) / ratio;
        flatgd = rtd * Math.Atan(tlat);

        return flatgd;

    }

    // =======================================================================

    private static double gd2gc(double flatgd, double altkm)
    /* geodetic latitude to geocentric latitude

         Input:
                   flatgd    geodetic latitude deg.
                   altkm     altitide in km
         ouput:
                   flatgc    geocentric latitude in deg
    */
    {

        double dtr = Math.PI / 180.0f;
        var rtd = 1 / dtr;

        double flatgc;
        double[] rrnrm = new double[3];
        double re, rn, ecc, esq;
        double slat, clat, tlat;
        double altnow, ratio;

        geodGBL();

        ecc = EARTH_Ecc;
        esq = ecc * ecc;

        altnow = altkm;

        rrnrm = radcur(flatgd);
        rn = rrnrm[1];

        ratio = 1 - esq * rn / (rn + altnow);

        tlat = Math.Tan(dtr * flatgd) * ratio;
        flatgc = rtd * Math.Atan(tlat);

        return flatgc;

    }

    // =======================================================================
    private static double[][] llenu(double flat, double flon)
    /* latitude longitude to east,north,up unit vectors

         input:
                   flat      latitude in degees N
                             [ gc -> gc enu,  gd usual enu ]
                   flon      longitude in degrees E
         output:
                   enu      List of 3, 3-unit vectors
    */
    {

        double dtr, clat, slat, clon, slon;
        double[] ee = new double[3];
        double[] en = new double[3];
        double[] eu = new double[3];

        double[][] enu = new double[3][];

        dtr = Math.PI / 180.0d;

        clat = Math.Cos(dtr * flat);
        slat = Math.Sin(dtr * flat);
        clon = Math.Cos(dtr * flon);
        slon = Math.Sin(dtr * flon);

        ee[0] = -slon;
        ee[1] = clon;
        ee[2] = 0.0d;

        en[0] = -clon * slat;
        en[1] = -slon * slat;
        en[2] = clat;

        eu[0] = clon * clat;
        eu[1] = slon * clat;
        eu[2] = slat;

        enu[0] = ee;
        enu[1] = en;
        enu[2] = eu;

        return enu;

    }

    // =======================================================================

    /// <summary>
    /// lat, lon, altitude to ECEF (Earth Centered, Earth Fixed) xyz location.
    /// </summary>
    /// <param name="flat">geodetic latitude in deg/param>
    /// <param name="flon">longitude in deg/param>
    /// <param name="altkm">altitude in km/param>
    /// <returns>xyz ECEF location as double[] in km</returns>
    private static double[] llhxyz(double flat, double flon, double altkm )
    {
        double dtr = Math.PI / 180.0d;
        double clat, clon, slat, slon;
        double[] rrnrm = new double[3];
        double rn, re, ecc, esq;
        double[] xvec = new double[3];

        geodGBL();

        clat = Math.Cos(dtr * flat);
        slat = Math.Sin(dtr * flat);
        clon = Math.Cos(dtr * flon);
        slon = Math.Sin(dtr * flon);

        rrnrm = radcur(flat);
        rn = rrnrm[1];
        re = rrnrm[0];

        ecc = EARTH_Ecc;
        esq = ecc * ecc;

        xvec[0] = (rn + altkm) * clat * clon;
        xvec[1] = (rn + altkm) * clat * slon;
        xvec[2] = ((1 - esq) * rn + altkm) * slat;

        return xvec;
    }

    // =======================================================================

    /// <summary>
    /// ECEF (Earth Centered, Earth Fixed) xyz location to lat, lon and altitide.
    /// </summary>
    /// <param name="xvec">xyz ECEF location in km</param>
    /// <returns>A float array of length 3 containing geodetic latitude in deg, longitude in deg and altitude in km in that order.</returns>
    private static double[] xyzllh(double[] xvec)
    {
        double flat, flon, altkm;
        double[] llhvec = new double[3];

        double dtr = Math.PI / 180.0d;
        double flatgc, flatn, dlat;
        double rnow, rp;
        double x, y, z, p;
        double tangc, tangd;

        double testval;

        double rn, esq;
        double clat, slat;
        double[] rrnrm = new double[3];

        geodGBL();

        esq = EARTH_Esq;

        x = xvec[0];
        y = xvec[1];
        z = xvec[2];

        rp = Math.Sqrt(x * x + y * y + z * z);

        flatgc = Math.Asin(z / rp) / dtr;

        testval = Math.Abs(x) + Math.Abs(y);
        if (testval < 1.0e-10)
        {
            flon = 0.0d;
        }
        else
        {
            flon = Math.Atan2(y, x) / dtr;
        }
        if (flon < 0.0)
        {
            flon = flon + 360.0d;
        }

        p = Math.Sqrt(x * x + y * y);

        // on pole special case
        if (p < 1.0e-10)
        {
            flat = 90.0d;
            if (z < 0.0)
            {
                flat = -90.0d;
            }

            altkm = rp - rearth(flat);
            llhvec[0] = flat;
            llhvec[1] = flon;
            llhvec[2] = altkm;

            return llhvec;
        }

        // first iteration, use flatgc to get altitude and alt needed to convert gc to gd lat.
        rnow = rearth(flatgc);
        altkm = rp - rnow;
        flat = gc2gd(flatgc, altkm);

        rrnrm = radcur(flat);
        rn = rrnrm[1];

        for (int kount = 0; kount < 5; kount++)
        {
            slat = Math.Sin(dtr * flat);
            tangd = (z + rn * esq * slat) / p;
            flatn = Math.Atan(tangd) / dtr;

            dlat = flatn - flat;
            flat = flatn;
            clat = Math.Cos(dtr * flat);

            rrnrm = radcur(flat);
            rn = rrnrm[1];

            altkm = (p / clat) - rn;

            if (Math.Abs(dlat) < 1.0e-12)
            {
                break;
            }

        }

        llhvec[0] = flat;
        llhvec[1] = flon;
        llhvec[2] = altkm;

        return llhvec;

    }

    // ======================================================================= All methods from this point added by Jen Laing 2020.

    /// <summary>
    /// geocentric lat, lon, altitude to ECEF (Earth Centered, Earth Fixed) xyz location.
    /// </summary>
    /// <param name="lat">geocentric latitude in deg/param>
    /// <param name="lon">longitude in deg/param>
    /// <param name="altkm">altitude in km/param>
    /// <returns>xyz ECEF location as Vector3 in km</returns>
    private static double[] LatLonAlt2ECEF(double lat, double lon, double altkm)
    {
        double geodecticLat = gc2gd(lat, altkm);
        double[] ECEFxyz = llhxyz(geodecticLat, lon, altkm);

        return ECEFxyz;
    }

    // =======================================================================

    /// <summary>
    /// ECEF (Earth Centered, Earth Fixed) xyz location to geocentric lat, lon and altitide.
    /// </summary>
    /// <param name="xvec">xyz ECEF location in km</param>
    /// <returns>A float array of length 3 containing geocentric latitude in deg, longitude in deg and altitude in km in that order.</returns>
    private static double[] ECEF2LatLonAlt(double[] xvec)
    {
        double[] geodeticLatLonAlt = new double[3];
        geodeticLatLonAlt = xyzllh(xvec);

        double[] geocentricLatLonAlt = new double[3];

        geocentricLatLonAlt[0] = gd2gc(geodeticLatLonAlt[0], geodeticLatLonAlt[2]);
        geocentricLatLonAlt[1] = geodeticLatLonAlt[1];
        geocentricLatLonAlt[2] = geodeticLatLonAlt[2];

        return geocentricLatLonAlt;

    }

    // =======================================================================

    /// <summary>
    /// Returns the three unit vectors of the tangent plane (that relates to the x-z horizontal plane in Unity) 
    /// for east, north and up at the given geocentric latitude, longitude and altitude point on the WGS84 ellipsoid.
    /// </summary>
    /// <param name="lat">Geocentric Latitude in degrees N</param>
    /// <param name="lon">Longitude in degrees E</param>
    /// <param name="altkm">Altitude in kilometres</param>
    /// <returns>An array of the three unit vectors of the tangent plane for east, north and up in that order.</returns>
    private static double[][] LatLonAlt2ENUUnitVectors(double lat, double lon, double altkm)
    {
        double geodeticLat = gc2gd(lat, altkm);

        double[][] ENUUnitVectors = llenu(geodeticLat, lon);

        return ENUUnitVectors;
    }

    // =======================================================================

    /// <summary>
    /// Local ENU distance vector from reference point to position in ECEF.
    /// </summary>
    /// <param name="ECEFAtRefPoint"> ECEF at the reference point/param>
    /// <param name="ENUUnitVectors"> ENU matrix at the reference point/param>
    ///  <param name="ENUDist"> Distance in enu (in Unity)/param>
    /// <returns>Vector3 ECEF position at ENUDist from ECEF ref point</returns>
    private static double[] ENUDist2ECEFPos(double[] ECEFAtRefPoint, double[][] ENUUnitVectorsAtRefPoint, double[] ENUDist)
    {
        double[] ECEFPosition = new double[3];

        ECEFPosition[0] = ENUUnitVectorsAtRefPoint[0][0] * ENUDist[0] + ENUUnitVectorsAtRefPoint[1][0] * ENUDist[1] + ENUUnitVectorsAtRefPoint[2][0] * ENUDist[2] + ECEFAtRefPoint[0];
        ECEFPosition[1] = ENUUnitVectorsAtRefPoint[0][1] * ENUDist[0] + ENUUnitVectorsAtRefPoint[1][1] * ENUDist[1] + ENUUnitVectorsAtRefPoint[2][1] * ENUDist[2] + ECEFAtRefPoint[1];
        ECEFPosition[2] = ENUUnitVectorsAtRefPoint[0][2] * ENUDist[0] + ENUUnitVectorsAtRefPoint[1][2] * ENUDist[1] + ENUUnitVectorsAtRefPoint[2][2] * ENUDist[2] + ECEFAtRefPoint[2];

        return ECEFPosition;
    }


    /// <summary>
    /// Calculates the ENU vector on the tangent plane at the ref point from the ECEF ref point to another ECEF point.
    /// </summary>
    /// <param name="ECEFAtRefPoint"> ECEF at the reference point/param>
    /// <param name="ECEFAtCurrentPoint"> ECEF at the current point/param>
    /// <param name="ENUUnitVectorsAtRefPoint"> ENU matrix that defines the tangent plane at the reference point/param>
    /// <returns>Vector difference in ENU</returns>
    private static double[] ECEFPos2ENUDist(double[] ECEFAtRefPoint, double[] ECEFAtCurrentPoint, double[][] ENUUnitVectorsAtRefPoint)
    {
        double[] ENUDist = new double[3];

        double[] ECEFDist = new double[3] { ECEFAtCurrentPoint[0] - ECEFAtRefPoint[0], ECEFAtCurrentPoint[1] - ECEFAtRefPoint[1], ECEFAtCurrentPoint[2] - ECEFAtRefPoint[2] };

        ENUDist[0] = ENUUnitVectorsAtRefPoint[0][0] * ECEFDist[0] + ENUUnitVectorsAtRefPoint[0][1] * ECEFDist[1] + ENUUnitVectorsAtRefPoint[0][2] * ECEFDist[2];
        ENUDist[1] = ENUUnitVectorsAtRefPoint[1][0] * ECEFDist[0] + ENUUnitVectorsAtRefPoint[1][1] * ECEFDist[1] + ENUUnitVectorsAtRefPoint[1][2] * ECEFDist[2];
        ENUDist[2] = ENUUnitVectorsAtRefPoint[2][0] * ECEFDist[0] + ENUUnitVectorsAtRefPoint[2][1] * ECEFDist[1] + ENUUnitVectorsAtRefPoint[2][2] * ECEFDist[2];

        return ENUDist;
    }

    // =======================================================================

    //Adjustments to correct for whatever north has been set to in scene
    private static double[] ENU2Unity(double[] enuVector)
    {
        if (rotAdjMatrix_ACW == null)
        {
            SetAdjMatrix();
        }
        double[] standardUnity = new double[3] { enuVector[1], enuVector[2], -enuVector[0] };
        double[] adjForNorthUnity = new double[3] { rotAdjMatrix_ACW[0, 0] * standardUnity[0] + rotAdjMatrix_ACW[0, 1] * standardUnity[2], standardUnity[1], rotAdjMatrix_ACW[1, 0] * standardUnity[0] + rotAdjMatrix_ACW[1, 1] * standardUnity[2] };

        return adjForNorthUnity;
    }
    private static Vector3 ENU2Unity(Vector3 enuVector)
    {
        if (rotAdjMatrix_ACW == null)
        {
            SetAdjMatrix();
        }
        Vector3 standardUnity = new Vector3(enuVector[1], enuVector[2], -enuVector[0]);
        Vector3 adjForNorthUnity = new Vector3(rotAdjMatrix_ACW[0, 0] * standardUnity[0] + rotAdjMatrix_ACW[0, 1] * standardUnity[2], standardUnity[1], rotAdjMatrix_ACW[1, 0] * standardUnity[0] + rotAdjMatrix_ACW[1, 1] * standardUnity[2]);

        return adjForNorthUnity;
    }
    private static double[] Unity2ENU(double[] unityVector)
    {
        if (rotAdjMatrix_ACW == null)
        {
            SetAdjMatrix();
        }
        double[] standardUnity = new double[3] { rotAdjMatrix_CW[0, 0] * unityVector[0] + rotAdjMatrix_CW[0, 1] * unityVector[2], unityVector[1], rotAdjMatrix_CW[1, 0] * unityVector[0] + rotAdjMatrix_CW[1, 1] * unityVector[2] };
        double[] enu = new double[3] { -standardUnity[2], standardUnity[0], standardUnity[1] };

        return enu;
    }
    private static Vector3 Unity2ENU(Vector3 unityVector)
    {
        if (rotAdjMatrix_ACW == null)
        {
            SetAdjMatrix();
        }
        Vector3 standardUnity = new Vector3 ( rotAdjMatrix_CW[0, 0] * unityVector[0] + rotAdjMatrix_CW[0, 1] * unityVector[2], unityVector[1], rotAdjMatrix_CW[1, 0] * unityVector[0] + rotAdjMatrix_CW[1, 1] * unityVector[2] );
        Vector3 enu = new Vector3 ( -standardUnity[2], standardUnity[0], standardUnity[1] );

        return enu;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Give an estimate of the latitude, longitude and altitude of a point on the tangent plane based on a known reference point.
    /// The ENU tangent plane is centred at the known reference point passed to the method, which means that the further away
    /// from this reference point the new point is, the less accurate the result will be due to the curvature of the earth.
    /// n.b. the result is the exact position on the tangent plane.
    /// </summary>
    /// <param name="latRefPoint"> Geocentric latitude (degrees) at the reference point/param>
    /// <param name="lonRefPoint"> Longitude (degrees) at the reference point/param>
    /// <param name="altRefPoint"> Altitude (distance above sea level in metres)at the reference point/param>
    /// <param name="refPointInUnity"> Vector3 reference point position in Unity (m from (0, 0, 0)/param>
    /// <param name="currentPositionInUnity"> Vector3 current point position in Unity(m from (0, 0, 0)/param>
    /// <returns>Estimate of geocentric latitude (degrees north), Longitude (degrees East), Altitude (metres) of current position in Unity</returns>
    public static double[] LatLonAltEstimate(double latRefPoint, double lonRefPoint, double altRefPoint, Vector3 refPointInUnity, Vector3 currentPositionInUnity)
    {
        double[] latLonAltEstimate = new double[3];

        Vector3 diff = Unity2ENU(currentPositionInUnity - refPointInUnity);
        double[] diffInKm = new double[3] { diff.X / 1000d, diff.Y / 1000d, diff.Z / 1000d, };
        double altKm = altRefPoint / 1000d;
        double[] ECEFRefPoint = LatLonAlt2ECEF(latRefPoint, lonRefPoint, altKm);
        double[][] ENUUnitVectorsAtRefPoint = LatLonAlt2ENUUnitVectors(latRefPoint, lonRefPoint, altKm);
        double[] ECEFCurrentPos = ENUDist2ECEFPos(ECEFRefPoint, ENUUnitVectorsAtRefPoint, diffInKm);

        latLonAltEstimate = ECEF2LatLonAlt(ECEFCurrentPos);
        latLonAltEstimate[2] *= 1000d;
        if (latLonAltEstimate[0] > 90)
        {
            latLonAltEstimate[0] = 180d - latLonAltEstimate[0];
        }
        if (latLonAltEstimate[0] < -90)
        {
            latLonAltEstimate[0] = -180d - latLonAltEstimate[0];
        }
        if (latLonAltEstimate[1] > 180)
        {
            latLonAltEstimate[1] -= 360d;
        }
        if (latLonAltEstimate[1] < -180)
        {
            latLonAltEstimate[1] += 360d;
        }

        return latLonAltEstimate;
    }


    public static double[] LatLonAltEstimate2(double latRefPoint, double lonRefPoint, double altRefPoint, Vector3 refPointInUnity, Vector3 currentPositionInUnity)
    {
        double[] latLonAltEstimate = new double[3];

        Vector3 diffInUnity = Unity2ENU(currentPositionInUnity - refPointInUnity);

        latLonAltEstimate[0] = latRefPoint + diffInUnity.Z / LengthOneDegOfLatInMetresAtRefPoint(latRefPoint);
        latLonAltEstimate[1] = lonRefPoint + diffInUnity.X / LengthOneDegOfLonInMetresAtRefPoint(latRefPoint);

        if (latLonAltEstimate[0] > 90)
        {
            latLonAltEstimate[0] = 180d - latLonAltEstimate[0];
        }
        if (latLonAltEstimate[0] < -90)
        {
            latLonAltEstimate[0] = -180d - latLonAltEstimate[0];
        }
        if (latLonAltEstimate[1] > 180)
        {
            latLonAltEstimate[1] -= 360d;
        }
        if (latLonAltEstimate[1] < -180)
        {
            latLonAltEstimate[1] += 360d;
        }

        return latLonAltEstimate;
    }

    public static double LengthOneDegOfLatInMetresAtRefPoint(double latAtRefPoint)
    {
        double latAtRefPointInRads = latAtRefPoint * Math.PI / 180d;
        double lengthInMetres = 111132.92d - 559.82d * Math.Cos(2 * latAtRefPointInRads) + 1.175d * Math.Cos(4d * latAtRefPointInRads) - 0.0023d * Math.Cos(6d * latAtRefPointInRads);

        return lengthInMetres;
    }

    public static double LengthOneDegOfLonInMetresAtRefPoint(double latAtRefPoint)
    {
        double latAtRefPointInRads = latAtRefPoint * Math.PI / 180d;
        double lengthInMetres = 111412.84d * Math.Cos(latAtRefPointInRads) - 93.5d * Math.Cos(3d * latAtRefPointInRads) + 0.118d * Math.Cos(5d * latAtRefPointInRads);

        return lengthInMetres;
    }

    public static Vector3 LatLonAltkm2UnityPos(double lat_refPoint, double lon_refPoint, double alt_refPoint, double lat_transformPoint, double lon_transformPoint, double alt_transformPoint, Vector3 unityPos_refPoint)
    {

        double[] ECEF_refPoint = LatLonAlt2ECEF(lat_refPoint, lon_refPoint, alt_refPoint);
        double[] ECEF_transformPoint = LatLonAlt2ECEF(lat_transformPoint, lon_transformPoint, alt_transformPoint);
        double[][] enuUnitVectors = LatLonAlt2ENUUnitVectors(lat_refPoint, lon_refPoint, alt_refPoint);
        double[] distInKm = ECEFPos2ENUDist(ECEF_refPoint, ECEF_transformPoint, enuUnitVectors);
        double[] distInMetres = new double[3] { 1000d * distInKm[0], 1000d * distInKm[1], 1000d * distInKm[2] };
        double[] transformedDist = ENU2Unity(distInMetres);


        Vector3 transformedPoint = new Vector3(unityPos_refPoint[0] + (float)transformedDist[0], unityPos_refPoint[1] + (float)transformedDist[1], unityPos_refPoint[2] + (float)transformedDist[2]);

        return transformedPoint;
    }




    #endregion

}
