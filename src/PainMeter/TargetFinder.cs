using System.Collections.Generic;
using UnityEngine;

namespace PainMeter
{
	/// <summary>
	/// Which entities get a meter. Vanilla API only, so the overhead modes work without Undead
	/// Legacy. The crosshair test is the same one UL's own target bar makes.
	/// </summary>
	internal static class TargetFinder
	{
		private static readonly List<Entity> scratch = new List<Entity>();

		/// <summary>An enemy that is up and fighting. Players never qualify; sleepers are left
		/// alone, as UL's health bar leaves them.</summary>
		internal static bool Qualifies(EntityAlive _entity)
		{
			if (_entity == null || !(_entity is EntityEnemy) || _entity is EntityPlayer)
			{
				return false;
			}
			if (!Settings.Animals && _entity is EntityEnemyAnimal)
			{
				return false;
			}
			return _entity.IsAlive() && !_entity.IsDead() && _entity.Health > 0 && !_entity.IsSleeping;
		}

		/// <summary>The entity under the crosshair, or null.</summary>
		internal static EntityAlive CrosshairTarget(EntityPlayerLocal _player)
		{
			WorldRayHitInfo hit = _player?.HitInfo;
			if (hit == null || !hit.bHitValid || hit.transform == null || hit.tag == null
				|| !hit.tag.StartsWith("E_"))
			{
				return null;
			}
			Transform rootTransform = GameUtils.GetHitRootTransform(hit.tag, hit.transform);
			if (rootTransform == null)
			{
				return null;
			}
			EntityAlive entity = rootTransform.GetComponent<EntityAlive>();
			return Qualifies(entity) ? entity : null;
		}

		/// <summary>Every qualifying entity within <paramref name="_range"/> metres of the player.</summary>
		internal static void Nearby(World _world, EntityPlayerLocal _player, float _range, List<EntityAlive> _out)
		{
			_out.Clear();
			if (_world == null || _player == null || _range <= 0f)
			{
				return;
			}
			EntityFlags mask = EntityFlags.Zombie | EntityFlags.Bandit;
			if (Settings.Animals)
			{
				mask |= EntityFlags.Animal;
			}
			scratch.Clear();
			_world.GetEntitiesAround(mask, _player.position, _range, scratch);
			for (int i = 0; i < scratch.Count; i++)
			{
				EntityAlive entity = scratch[i] as EntityAlive;
				if (Qualifies(entity) && Vector3.Distance(entity.position, _player.position) <= _range)
				{
					_out.Add(entity);
				}
			}
			scratch.Clear();
		}
	}
}
