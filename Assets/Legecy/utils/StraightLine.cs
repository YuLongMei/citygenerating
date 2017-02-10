using UnityEngine;

public class StraightLine {

    private Vector3 point;
    private Vector3 direction;

    public StraightLine(Vector3 point, Vector3 dir)
    {
        this.point = point;
        this.direction = dir;
    }

    public Vector3? getIntersectionWithPlane(Plane p)
    {
        Ray r = new Ray(point, direction);
        float d;

        if (p.Raycast(r, out d))
        {
            return r.GetPoint(d);
        }
        else
        {
            r = new Ray(point, -direction);

            if (p.Raycast(r, out d))
            {
                return r.GetPoint(d);
            }
            else
            {
                return null;
            }
        }
    }

    public Vector3? getIntersectionWithX(float x)
    {
        Plane p = new Plane(Vector3.right, Vector3.right * x);
        return getIntersectionWithPlane(p);
    }

    public Vector3? getIntersectionWithY(float y)
    {
        Plane p = new Plane(Vector3.up, Vector3.up * y);
        return getIntersectionWithPlane(p);
    }

    public Vector3? getIntersectionWithZ(float z)
    {
        Plane p = new Plane(Vector3.forward, Vector3.forward * z);
        return getIntersectionWithPlane(p);
    }

    public Quaternion RotationIn2D
    {
        get
        {
            return Quaternion.Euler(0, -Mathf.Atan(direction.z / direction.x) * Mathf.Rad2Deg, 0);
        }
    }
}
