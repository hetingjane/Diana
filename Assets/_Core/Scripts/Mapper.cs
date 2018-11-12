using UnityEngine;

public struct Quadrilateral
{
    public readonly Vector2 a;
    public readonly Vector2 b;
    public readonly Vector2 c;
    public readonly Vector2 d;

    public Quadrilateral(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }
}

public class Mapper
{
    private Matrix4x4 projection;

    private Matrix4x4 GetScaledMatrix(Quadrilateral q)
    {
        Matrix4x4 inMatrix = new Matrix4x4(
            new Vector4(q.a.x, q.a.y, 0f, 1f),
            new Vector4(q.b.x, q.b.y, 0f, 1f),
            new Vector4(0f, 0f, 1f, 0f),
            new Vector4(q.c.x, q.c.y, 0f, 1f)
        );
        
        Vector4 scales = inMatrix.inverse * new Vector4(q.d.x, q.d.y, 1f, 1f);
        for (int i = 0; i < 4; i++)
            inMatrix.SetColumn(i, inMatrix.GetColumn(i) * scales[i]);
            
        return inMatrix;
    }

    public Mapper(Quadrilateral source, Quadrilateral destination)
    {
        Matrix4x4 A = GetScaledMatrix(source);
        Matrix4x4 B = GetScaledMatrix(destination);
        projection = B * A.inverse;
    }

    public Vector2 Map(Vector2 point)
    {
        return Map(point.x, point.y);
    }

    public Vector2 Map(float x, float y)
    {
        // MultiplyPoint internally makes Vector3 into homogenous coordinates by appending 1
        // and also dividing by 'w' at the end
        Vector3 pt = projection.MultiplyPoint(new Vector3(x, y, 0f));
        Debug.Log(pt);
        return pt;
    }
}
