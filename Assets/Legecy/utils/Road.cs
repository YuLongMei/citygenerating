using UnityEngine;
using System.Collections;

public class RoadInfo {

    public Vector3 head = Vector3.zero;
    public Vector3 tail = Vector3.zero;

    public float width = 0f;
    public Quaternion rotation = Quaternion.identity;

    private Ray? lineSegment = null;

    public float Length
    {
        get
        {
            return Vector3.Distance(head, tail);
        }
    }

    public Vector3 Position
    {
        get
        {
            return 0.5f * (head + tail);
        }
    }

    public Ray LineSegment
    {
        get
        {
            if (lineSegment == null)
            {
                lineSegment = new Ray(head, Direction);
            }
            return lineSegment.Value;
        }
    }

    public Vector3 Direction
    {
        get
        {
            return tail - head;
        }
    }
}
