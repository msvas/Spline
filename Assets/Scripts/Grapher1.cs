using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;

public class Grapher1 : MonoBehaviour {
    private int pointsTotal = 4;

    public TextAsset curvePoints;

    public Camera maincamera;

    private ParticleSystem.Particle[] radiusPoints;

    [Range(10, 100)]
    public int resolution = 10;

    private LineRenderer line;                      // line rendered on visual space
    private List<Vector3> linePoints;               // all points rendered on space, after catmull-rom processing
    private List<Vector3> pointsFromFile;           // simple points read from file
    private List<float> allRadius;                  // radius from each set of 3 points
    private List<float> allAngles;                  // angles between radius vector and 0, 0, -1

    private List<Vector3> radiusPos;
    private List<Vector3> midPoints;

    public float angleThreshold = 3;

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

        Vector3 midp1 = (((p2 - p1) / 2f) + p1);
        Vector3 midp2 = (((p3 - p2) / 2f) + p2);
        Debug.Log("p1: " + p1.x + " " + p1.y);
        Debug.Log("p2 " + p2.x + " " + p2.y);
        Debug.Log("p3 " + p3.x + " " + p3.y);
        Debug.Log("m1 " + midp1.x + " " + midp1.y);
        Debug.Log("m2 " + midp2.x + " " + midp2.y);
        Vector3 normal = Vector3.Cross(Vector3.Normalize(p2 - midp1), Vector3.Normalize(p3 - midp2));

        //Debug.Log("eixo" + normal.normalized);
        //Debug.Log("eixo" + (p3 - midp2));
        Vector3 axis1 = Vector3.Cross(Vector3.Normalize(p2 - midp1), normal.normalized);
        Vector3 axis2 = Vector3.Cross(Vector3.Normalize(p3 - midp2), normal.normalized);

        if (LineLineIntersection(out intersection, midp1, axis1, midp2, axis2)) {
            radius = Vector3.Magnitude(intersection - midp1);
            //Debug.Log("radius:" + radius);
            //Debug.Log("eixo" + axis1);
            allRadius.Add(radius);
            allAngles.Add(Vector3.Angle(intersection - midp1, Vector3.back));
            radiusPos.Add(intersection);
            midPoints.Add(midp1);
            midPoints.Add(midp2);
        }
        return radius;
    }
    
    //Calculate the intersection point of two lines. Returns true if lines intersect, otherwise false.
    private bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2) {

        /* 
        Given two lines passing through 3D points r1=[r1x,r1y,r1z] and r2=[r2x,r2y,r2z] and 
        having unit directions e1=[e1x,e1y,e1z] and e2=[e2x,e2y,e2z] 
        you can find the points on the line which are closest to the other line like this:

        Find the direction projection u=Dot(e1,e2)=e1x*e2x+e1y*e2y+e1z*e2z
        If u==1 then lines are parallel. No intersection exists.
        Find the separation projections t1=Dot(r2-r1,e1) and t2=Dot(r2-r1,e2)
        Find distance along line1 d1 = (t1-u*t2)/(1-u*u)
        Find distance along line2 d2 = (t2-u*t1)/(u*u-1)
        Find the point on line1 p1=Add(r1,Scale(d1,e1))
        Find the point on line2 p2=Add(r2,Scale(d2,e2))

        Note: You must have the directions as unit vectors, Dot(e1,e1)=1 and Dot(e2,e2)=1. 
        The function Dot() is the vector dot product. The function Add() adds the components 
        of vectors, and the function Scale() multiplies the components of the vector with a number.
        */

        float projection = Vector3.Dot(lineVec1.normalized, lineVec2.normalized);
        //Debug.Log("proj:" + lineVec1.normalized);
        if (projection != 1) {
            float t1 = Vector3.Dot((linePoint2 - linePoint1), lineVec1.normalized);
            float t2 = Vector3.Dot((linePoint2 - linePoint1), lineVec2.normalized);
            float d1 = (t1 - (projection * t2)) / (1 - (projection * projection));
            float d2 = (t2 - (projection * t1)) / ((projection * projection) - 1);
            intersection = linePoint1 + (lineVec1.normalized * d1);
            Debug.Log("I:" + intersection);
            return true;
        } else {
            intersection = Vector3.zero;
            return false;
        }

        /*
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
        */
    }

    public void GetAllRadius() {
        allRadius = new List<float>();
        allAngles = new List<float>();
        radiusPos = new List<Vector3>();
        midPoints = new List<Vector3>();
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
            angle = Vector3.Angle(firstVec.normalized, secondVec.normalized);

            if (angle > angleThreshold) {
                int l = m + 1;
                firstPoint = linePoints[l];

                while (angle > angleThreshold && l < (linePoints.Count - 2)) {
                    firstVec = linePoints[l + 1] - linePoints[l];
                    secondVec = linePoints[l + 2] - linePoints[l + 1];
                    angle = Vector3.Angle(firstVec.normalized, secondVec.normalized);
                    secondPoint = linePoints[(m + 1) + Mathf.FloorToInt(((l + 2) - (m + 1)) / 2)];
                    thirdPoint = linePoints[l + 2];
                    l++;
                }

                GetRadius(firstPoint, secondPoint, thirdPoint);
                m = l - 1;
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
        radiusPoints = new ParticleSystem.Particle[allRadius.Count + midPoints.Count];
        for(int i = 0; i < allRadius.Count; i++) {
            radiusPoints[i].position = radiusPos[i];
            Debug.Log("printing: " + radiusPos[i].x + " " + radiusPos[i].y);
            radiusPoints[i].startColor = new Color(10f, 0f, 0f);
            radiusPoints[i].startSize = 0.5f;
        }

        for (int i = allRadius.Count; i < allRadius.Count + midPoints.Count; i++) {
            radiusPoints[i].position = midPoints[i - allRadius.Count];
            radiusPoints[i].startColor = new Color(0f, 50f, 0f);
            radiusPoints[i].startSize = 0.5f;
        }
        GetComponent<ParticleSystem>().SetParticles(radiusPoints, radiusPoints.Length);

    }

    private void SaveFile() {
        string content = "";
        for (int i = 0; i < allRadius.Count; i++) {
            content += radiusPos[i].x + " " + radiusPos[i].y + " " + radiusPos[i].z + " " + allRadius[i] + " " + allAngles[i] + Environment.NewLine;
        }
        System.IO.File.WriteAllText("output.txt", content);
    }

    private void UpdateCamera() {
        maincamera.transform.position = linePoints[0] + Vector3.one;
        maincamera.transform.LookAt(linePoints[0]);
    }

    void Update() {
        if (linePoints.Count < (pointsTotal - 3) * resolution) {
            PerformSpline();
            GetAllRadius();
            PlotRadius();
            SaveFile();
            UpdateCamera();
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
