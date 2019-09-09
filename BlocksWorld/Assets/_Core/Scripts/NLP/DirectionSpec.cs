using System;
namespace Semantics
{
	public class DirectionSpec {
		public enum Direction {
			Unknown,
			WhereUserPoints
		}
		
		public Direction direction;
		
		public DirectionSpec(Direction dir) {
			this.direction = dir;
		}
		
		public override string ToString() {
			return string.Format("[{0}]", direction);
		}
	}
}
