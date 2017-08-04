using System.Collections.Generic;

public class Agent {
	public virtual void SendParameters(EnvironmentParameters env) {

	}

	public virtual string Receive() {
		return "";
	}

	public virtual float[] GetAction() {
		return new float[1] { 0.0f };
	}

	public virtual float[] GetValue() {
		float[] value = new float[1];
		return value;
	}
		
	public virtual void SendState(List<float> state, float reward, bool d) {

	}
}
