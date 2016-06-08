using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;

public class Grapher1 : MonoBehaviour {
    private int pointsTotal = 4;

    public TextAsset curvePoints;

    private ParticleSystem.Particle[] radiusPoints;

    [Range(10, 100)]
    public int resolution = 10;

    private LineRenderer line;                      // line rendered on visual space
    private List<Vector3> linePoints;               // all points rendered on space, after catmull-rom processing
    private List<Vector3> pointsFromFile;           // simple points read from file
    private List<float> allRadius;                  // radius from each set of 3 points
    private List<Vector3> radiusPos;

    private float angleThreshold = 3;

    private bool ready = false;

    void Start() {
        curvePoints = Resources.Load("pointsdata") as TextAsset;
        linePoints = new List<Vector3>();
        pointsFromFile = new List<Vector3>();

        line = gameObject.AddComponent<LineRenderer>();
        line.SetColors(Color.yellow, Color.red);
        line.SetWidth(0.2F, 0.2F);
    }

    private void CreatePoint(float x, float y, float z) {
        Vector3 newPoint = new Vector3(x, y, z);
        linePoints.Add(newPoint);
        line.SetPosition(linePoints.Count - 1, newPoint);
    }

    private void ReadPointsFromFile() {
        string text = curvePoints.text;
        string[] fLines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

        pointsTotal = fLines.Length;
        line.SetVertexCount((pointsTotal - 3) * resolution);

        for (int i = 0; i < fLines.Length; i++) {
            string valueLine = fLines[i];
            string[] pointsRead = Regex.Split(valueLine, @"\s+");
            pointsFromFile.Add(new Vector3(float.Parse(pointsRead[0]), float.Parse(pointsRead[1]), float.Parse(pointsRead[2])));
        }
    }

    private Vector3 calculatePoints(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
        Vector3 a = 0.5f * (2f * p1);
        Vector3 b = 0.5f * (p2 - p0);
        Vector3 c = 0.5f * (2f * p0 - 5f * p1 + 4f * p2 - p3);
        Vector3 d = 0.5f * (-p0 + 3f * p1 - 3f * p2 + p3);

        Vector3 pos = a + (b * t) + (c * t * t) + (d * t * t * t);

        return pos;
    }

    private void CatmullRom(int id) {
        Vector3 p0 = pointsFromFile[id - 1];
        Vector3 p1 = pointsFromFile[id];
        Vector3 p2 = pointsFromFile[id + 1];
        Vector3 p3 = pointsFromFile[id + 2];

        Vector3 lastPos = new Vector3(0, 0, 0);

        float increment = (1f / resolution);

        float t = 0;
        for (int i = 0; i < resolution; i++) {
            Vector3 newPos = calculatePoints(t, p0, p1, p2, p3);
            CreatePoint(newPos.x, newPos.y, newPos.z);
            t += increment;
        }
    }

    private void PerformSpline() {
        ReadPointsFromFile();

        ready = true;
 
        for (int i = 0; i < pointsTotal; i++) {
            if ((i != 0 && i != (pointsTotal - 2) && i != (pointsTotal - 1))) {
                CatmullRom(i);
            }
        }
    }

    public Vector3 GetPoint(float t) {
        if (ready) {
            int segmentNumber = Mathf.FloorToInt(t);

            if (segmentNumber > pointsFromFile.Count - 3)
                return new Vector3(0, 0, 0);

            if ((segmentNumber != 0 && segmentNumber != (pointsTotal - 2) && segmentNumber != (pointsTotal - 1))) {

                Vector3 p0 = pointsFromFile[segmentNumber - 1];
                Vector3 p1 = pointsFromFile[segmentNumber];
                Vector3 p2 = pointsFromFile[segmentNumber + 1];
                Vector3 p3 = pointsFromFile[segmentNumber + 2];

                return calculatePoints(t - segmentNumber, p0, p1, p2, p3);
            }
            else
                return new Vector3(0, 0, 0);
        } else {
            return new Vector3(0, 0, 0);
        }
    }

    private float GetRadius(Vector3 p1, Vector3 p2, Vector3 p3) {
        float radius = 0;
        Vector3 intersection = new Vector3();

        Vector3 normal = Vector3.Cross(p2 - p1, p3 - p2);
        Vector3 midp1 = 0.5f * (p1 + p2);
        Vector3 midp2 = 0.5f * (p2 + p3);

        Vector3 axis1 = Vector3.Cross((p2 - p1), normal);
        Vector3 axis2 = Vector3.Cross((p3 - p2), normal);

        if (LineLineIntersection(out intersection, midp1, axis1, midp2, axis2)) {
            radius = Vector3.Magnitude(intersection - midp1);
            Debug.Log("int:" + intersection);
            radiusPos.Add(intersection);
        }
        return radius;
    }
    
    //Calculate the intersection point of two lines. Returns true if lines intersect, otherwise false.
    private bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2) {

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //is coplanar, and not parrallel
        if (Mathf.Abs(planarFactor) < 0.0001f) { // && crossVec1and2.sqrMagnitude > 0.0001f) {
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
            intersection = linePoint1 + (lineVec1 * s);
            return true;
        } else {
            intersection = Vector3.zero;
            return false;
        }
    }

    public void GetAllRadius() {
        allRadius = new List<float>();
        radiusPos = new List<Vector3>();
        Vector3 firstVec = new Vector3(0, 0, 0);
        Vector3 secondVec = new Vector3(0, 0, 0);
        Vector3 firstPoint = new Vector3(0, 0, 0);
        Vector3 secondPoint = new Vector3(0, 0, 0);
        Vector3 thirdPoint = new Vector3(0, 0, 0);
        float angle = 0;
        int m = 0;
        while (m < (linePoints.Count - 2)) { 
            firstVec = linePoints[m + 1] - linePoints[m];
            secondVec = linePoints[m + 2] - linePoints[m + 1];
            angle = Vector3.Angle(firstVec, secondVec);
            if (angle > angleThreshold) {
                firstPoint = linePoints[m];
                int l = m + 2;
                while (angle > angleThreshold && l < (linePoints.Count - 2)) {
                    firstVec = linePoints[l + 1] - linePoints[l];
                    secondVec = linePoints[l + 2] - linePoints[l + 1];
                    angle = Vector3.Angle(firstVec, secondVec);
                    secondPoint = linePoints[m + Mathf.FloorToInt((l - m) / 2)];
                    thirdPoint = linePoints[l];
                    l++;
                }
                float radius = GetRadius(firstPoint, secondPoint, thirdPoint);
                Debug.Log(firstPoint);
                Debug.Log(secondPoint);
                Debug.Log(thirdPoint);
                Debug.Log("res: " + radius);
                if (radius != 0) {
                    allRadius.Add(radius);
                }
                m = l;
            }
            m++;
        }
        /*
        while (k != linePoints.Count) {
            float radius = GetRadius(linePoints[i], linePoints[Mathf.FloorToInt(k / 2)], linePoints[k]);

            if(radius == 0) {
                k++;
            } else {
                
                Debug.Log(linePoints[i]);
                Debug.Log(linePoints[Mathf.FloorToInt(k / 2)]);
                Debug.Log(linePoints[k]);
                Debug.Log("res: " + radius);
                
                allRadius.Add(radius);
                i = k;
            }
        }
       */
    }

    private void PlotRadius() {
        radiusPoints = new ParticleSystem.Particle[allRadius.Count];
        for(int i = 0; i < allRadius.Count; i++) {
            radiusPoints[i].position = radiusPos[i];
            radiusPoints[i].startColor = new Color(10f, 0f, 0f);
            radiusPoints[i].startSize = 0.5f;
        }
        GetComponent<ParticleSystem>().SetParticles(radiusPoints, radiusPoints.Length);

    }

    void Update() {
        if (linePoints.Count < (pointsTotal - 3) * resolution) {
            PerformSpline();
            GetAllRadius();
            PlotRadius();
        }
        /*
        Vector3 intPnt = new Vector3();
        Vector3 p1 = new Vector3(0, 0, 0);
        Vector3 p2 = new Vector3(1, 1, 0);
        Vector3 p3 = new Vector3(3, 0, 0);
        Vector3 midp1 = 0.5f * (p1 + p2);
        Vector3 midp2 = 0.5f * (p2 + p3);
        Vector3 normal = Vector3.Cross(p2 - p1, p3 - p2);
        Vector3 axis1 = Vector3.Cross((p2 - p1), normal);
        Vector3 axis2 = Vector3.Cross((p3 - p2), normal);
        LineLineIntersection(out intPnt, midp1, axis1, midp2, axis2);
        Debug.Log("Res: " + intPnt);
        */
    }
}
