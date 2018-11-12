using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class Table: MonoBehaviour {

    public readonly float DefaultRadius = .2f;

    private Collider coll;
    
    // Use this for initialization
    void Start () {
        coll  = gameObject.GetComponent<Collider>();
        if (coll == null)
            Debug.LogError("No collider found on the table");
    }

    public float Height
    {
        get
        {
            return coll.bounds.max.y;
        }
    }

    public Vector4 SurfaceBounds
    {
        get
        {
            Vector3 min = coll.bounds.min;
            Vector3 max = coll.bounds.max;
            return new Vector4(min.x, max.x, min.z, max.z);
        }
    }

    public Vector2 Center
    {
        get
        {
            Vector4 bounds = SurfaceBounds;
            return new Vector2((bounds.x + bounds.y) / 2f, (bounds.z + bounds.w) / 2f);
        }
    }

    public IList<GameObject> Blocks
    {
        get
        {
            Vector4 bounds = SurfaceBounds;
            float l1 = Mathf.Abs(bounds.x - bounds.y);
            float l2 = Mathf.Abs(bounds.z - bounds.w);
            float radius = l1 > l2 ? l1 : l2;
            return BlocksWithin(Center, radius);
        }
    }

    public IList<GameObject> BlocksWithin(Vector2 position, float radius, bool sort)
    {
        Collider[] colliders = Physics.OverlapSphere(new Vector3(position.x, Height, position.y), radius, LayerMask.GetMask("Blocks"));
        var blocks = new List<GameObject>();
        foreach (Collider c in colliders)
        {
            blocks.Add(c.gameObject);
        }
        if (sort)
        {
            blocks.Sort((b1, b2) => Vector3.SqrMagnitude(b1.transform.position) < Vector3.SqrMagnitude(b2.transform.position) ? -1 : 1);
        }
        return blocks;
    }
	
    public IList<GameObject> BlocksWithin(Vector2 position, float radius)
    {
        return BlocksWithin(position, radius, false);
    }

    public IList<GameObject> BlocksNear(Vector2 position)
    {
        return BlocksWithin(position, DefaultRadius);
    }

    public IList<GameObject> BlocksNear(Vector2 position, bool sort)
    {
        return BlocksWithin(position, DefaultRadius, sort);
    }

    public static GameObject GetBlockHandle(GameObject block)
    {
        Block b = block.GetComponent<Block>();
        if (b != null)
            return b.Handle;
        else
            return null;
    }

    public Vector2 ToTableCoordinates(Vector2 position)
    {
        Vector4 bounds = SurfaceBounds;
        Vector2 tablePosition = new Vector2();

        tablePosition.x = (position.x - bounds.x) / (bounds.y - bounds.x);
        tablePosition.y = (position.y - bounds.z) / (bounds.w - bounds.z);
        return tablePosition;
    }
}
