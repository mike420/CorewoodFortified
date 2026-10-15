namespace CorewoodFortified;

internal sealed class NetBarrier : StaticTarget, IDestructible
{
	public ChainNet Net;

	private void Awake()
	{
		m_primaryTarget = false;
		m_randomTarget = true;
	}

	public void Damage(HitData hit)
	{
		if (Net != null)
		{
			Net.TakeHit(hit);
		}
	}

	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Default;
	}
}
