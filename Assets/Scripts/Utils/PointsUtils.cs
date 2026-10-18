
using Effects;
using UnityEngine;

public static class PointsUtils
{
    public const float TICK_RATE = 1 / 40f;
    public const float INVERSE_TICK_RATE = 1 / TICK_RATE;

    public static void UpdateChain(this BodyConnection[] connections)
    {
        for(int i = 0; i < connections.Length; i++)
        {
            connections[i].Update();
        }
    }

    public static float Distance(this IReadOnlyBodyPoint point1, IReadOnlyBodyPoint point2)
    {
        return Vector2.Distance(point1.Position, point2.Position);
    }

    public static Vector2 Center(this IReadOnlyBodyPoint p1, IReadOnlyBodyPoint p2)
    {
        return (p1.Position + p2.Position) * 0.5f;
    }

    public static Vector2 Center(this IReadOnlyBodyPoint p1, Vector2 p2)
    {
        return (p1.Position + p2) * 0.5f;
    }

    public static Vector2 GetDirectionNormalized(this IReadOnlyBodyPoint from, IReadOnlyBodyPoint to, out float distance)
    {
        Vector2 dir = to.Position - from.Position;
        distance = dir.magnitude;
        return dir /= distance;
    }

    public static Vector2 GetDirectionNormalized(this IReadOnlyBodyPoint from, IReadOnlyBodyPoint to) => GetDirectionNormalized(from, to, out _);

    public static Vector2 GetDirection(this IReadOnlyBodyPoint from, IReadOnlyBodyPoint to)
    {
        return to.Position - from.Position;
	}

	public static Vector2 GetDirection(this IReadOnlyBodyPoint from, Vector2 to)
	{
		return to - from.Position;
	}

	public static Vector2 GetDirectionNormalized(this IReadOnlyBodyPoint from, Vector2 to, out float distance)
	{
		Vector2 dir = to - from.Position;
		distance = dir.magnitude;
		return dir /= distance;
	}

	public static Vector2 GetDirectionNormalized(this IReadOnlyBodyPoint from, Vector2 to) => GetDirectionNormalized(from, to, out _);

	public static Vector2 PerpendicularClockWise(this IReadOnlyBodyPoint from, IReadOnlyBodyPoint to)
    {
        Vector2 dir = to.Position - from.Position;
        return new Vector2(dir.y, -dir.x);
    }

    public static Vector2 PerpendicularCounterClockWise(this IReadOnlyBodyPoint from, IReadOnlyBodyPoint to)
    {
        Vector2 dir = to.Position - from.Position;
        return new Vector2(-dir.y, dir.x);
    }

    public static Vector2 PerpendicularClockWiseNormalized(this IReadOnlyBodyPoint from, IReadOnlyBodyPoint to)
    {
        Vector2 dir = to.Position - from.Position;
        return new Vector2(dir.y, -dir.x).normalized;
    }

    public static Vector2 PerpendicularClockWiseNormalized(this IReadOnlyBodyPoint from, IReadOnlyBodyPoint to, out float distance)
    {
        Vector2 dir = to.Position - from.Position;
        distance = dir.magnitude;
        return new Vector2(dir.y, -dir.x) / distance;
    }

    public static Vector2 PerpendicularCounterClockWiseNormalized(this IReadOnlyBodyPoint from, IReadOnlyBodyPoint to)
    {
        Vector2 dir = to.Position - from.Position;
        return new Vector2(-dir.y, dir.x).normalized;
    }

    public static Vector2 PerpendicularCounterClockWiseNormalized(this IReadOnlyBodyPoint from, IReadOnlyBodyPoint to, out float distance)
    {
        Vector2 dir = to.Position - from.Position;
        distance = dir.magnitude;
        return new Vector2(-dir.y, dir.x) / distance;
    }
}