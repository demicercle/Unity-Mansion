using UnityEngine;

public static class AnimatorUtils
{
	static public void TrySetFloat(string param, float value, params Animator[] animators)
	{
		if(!string.IsNullOrEmpty(param)) foreach(var animator in animators) animator.SetFloat(param, value);
	}
	static public void TrySetInteger(string param, int value, params Animator[] animators)
	{
		if(!string.IsNullOrEmpty(param)) foreach(var animator in animators) animator.SetInteger(param, value);
	}
	static public void TrySetBool(string param, bool value, params Animator[] animators)
	{
		if(!string.IsNullOrEmpty(param)) foreach(var animator in animators) animator.SetBool(param, value);
	}
	static public void TrySetTrigger(string param, params Animator[] animators)
	{
		if(!string.IsNullOrEmpty(param)) foreach(var animator in animators) animator.SetTrigger(param);
	}
	static public void TryResetTrigger(string param, params Animator[] animators)
	{
		if(!string.IsNullOrEmpty(param)) foreach(var animator in animators) animator.ResetTrigger(param);
	}
	
	static public void TrySetFloat(int param, float value, params Animator[] animators)
	{
		foreach (var animator in animators)
		{
			animator.SetFloat(param, value);
		}
	}
	static public void TrySetInteger(int param, int value, params Animator[] animators)
	{
		foreach (var animator in animators)
		{
			animator.SetInteger(param, value);
		}
	}
	static public void TrySetBool(int param, bool value, params Animator[] animators)
	{
		foreach (var animator in animators)
		{
			animator.SetBool(param, value);
		}
	}
	static public void TrySetTrigger(int param, params Animator[] animators)
	{
		foreach (var animator in animators)
		{
			animator.SetTrigger(param);
		}
	}
	static public void TryResetTrigger(int param, params Animator[] animators)
	{
		foreach (var animator in animators)
		{
			animator.ResetTrigger(param);
		}
	}
	
	/*static public void TrySetFloat(int param, float value, params Animator[] animators)
	{
		foreach (var animator in animators)
		{
			if (AnimatorHasParameter(animator, param))
			{
				animator.SetFloat(param, value);
			}
		}
	}
	static public void TrySetInteger(int param, int value, params Animator[] animators)
	{
		foreach (var animator in animators)
		{
			if (AnimatorHasParameter(animator, param))
			{
				animator.SetInteger(param, value);
			}
		}
	}
	static public void TrySetBool(int param, bool value, params Animator[] animators)
	{
		foreach (var animator in animators)
		{
			if (AnimatorHasParameter(animator, param))
			{
				animator.SetBool(param, value);
			}
		}
	}
	static public void TrySetTrigger(int param, params Animator[] animators)
	{
		foreach (var animator in animators)
		{
			if (AnimatorHasParameter(animator, param))
			{
				animator.SetTrigger(param);
			}
		}
	}
	static public void TryResetTrigger(int param, params Animator[] animators)
	{
		foreach (var animator in animators)
		{
			if (AnimatorHasParameter(animator, param))
			{
				animator.ResetTrigger(param);
			}
		}
	}*/

	static public bool AnimatorHasParameter(Animator animator, int param) => param < animator.parameterCount;
}