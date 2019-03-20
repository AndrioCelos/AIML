using System;
using System.Collections.Generic;
using System.Text;

namespace Aiml.ResponseParts {
	public class Delay : IResponsePart {
		public TimeSpan DelayTime { get; }

		public Delay(double seconds) {
			this.DelayTime = TimeSpan.FromSeconds(seconds);
		}
		public Delay(TimeSpan delayTime) {
			this.DelayTime = delayTime;
		}
	}
}
