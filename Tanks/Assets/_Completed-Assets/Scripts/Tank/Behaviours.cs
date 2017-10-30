using UnityEngine;
using NPBehave;
using System.Collections.Generic;

namespace Complete
{
	/*
    Example behaviour trees for the Tank AI.  This is partial definition:
    the core AI code is defined in TankAI.cs.

    Use this file to specifiy your new behaviour tree.
     */

	public partial class TankAI : MonoBehaviour
	{
		private Vector3 lastEnemyPos = new Vector3 (0, 0, 0);
		public float perceptionUpdateTime=0.05f;
		public float turnPrecision = 0.05f;
		private float timeOfLastCheck = 0;
		private float maxTurn;

		private Root CreateBehaviourTree ()
		{
			maxTurn=gameObject.GetComponent<TankMovement>().m_TurnSpeed;

			switch (m_Behaviour) {

			case 1:
				return TrackBehaviour ();
			//case 2:
				//return DeadlyBehaviour ();
			//case 3:
			//return FrightenedBehaviour ();

			default:
				return new Root (new Action (() => Turn (0.1f)));
			}
		}

		/* Actions */

		private Node StopTurning ()
		{
			return new Action (() => Turn (0));
		}

		private Node RandomFire ()
		{
			return new Action (() => Fire (UnityEngine.Random.Range (0.0f, 1.0f)));
		}
		private Node RangedFire ()
		{
			return new Action (() => CalculatePrediction ());
		}

		private void CalculatePrediction(){
			Vector3 targetPos = TargetTransform ().position; 
			Vector3 localPos = this.transform.InverseTransformPoint (targetPos);
			Vector3 localDir = this.transform.InverseTransformDirection (targetPos);
			Vector3 localVel = (targetPos - lastEnemyPos)/(Time.fixedTime-timeOfLastCheck);
			float DTT = localPos.magnitude; //Distance to target
			//float requiredSpd = Mathf.Sqrt (DTT* 15.0f); //Very simplified prediction model based on a constant elevated shot start point and a 10 degree angle
			float requiredSpd = Mathf.Sqrt (DTT * 15.0f) + localVel.magnitude;// * Mathf.Cos (localDir);

			//TODO: add a prediction point ahead of target


			Debug.Log ("Speed= " + requiredSpd+ "EnemySpeed= "+localVel.magnitude + "Direction= " +localDir);

			float time = 0.0f;
			if (requiredSpd > 25.0f)
				time = 1.0f;
			else if (requiredSpd > 15.0f)
				time = (requiredSpd -15.0f)/10.0f;
			Fire (time);

			lastEnemyPos = targetPos;
			timeOfLastCheck=Time.fixedTime;
		}

		/* Example behaviour trees */

		// Constantly spin and fire on the spot
		private Root SpinBehaviour (float turn, float shoot)
		{
			return new Root (new Sequence (
				new Action (() => Turn (turn)),
				new Action (() => Fire (shoot))
			));
		}

		// Turn to face your opponent and fire
		private Root TrackBehaviour ()
		{
			return new Root (
				new Service (perceptionUpdateTime, UpdatePerception,
					new Selector (
						new BlackboardCondition ("targetOffCentre", Operator.IS_SMALLER_OR_EQUAL, turnPrecision, Stops.IMMEDIATE_RESTART,
                            // Stop turning and fire
							new Sequence (StopTurning (),
								RangedFire ())),
						//Checks if the target is to the right 
						new BlackboardCondition ("targetFarRight", Operator.IS_EQUAL, true, Stops.IMMEDIATE_RESTART,
							// Turn left toward target
							new Action (() => Turn (maxTurn))),
						new BlackboardCondition ("targetFarLeft", Operator.IS_EQUAL, true, Stops.IMMEDIATE_RESTART,
							// Turn left toward target
							new Action (() => Turn (-maxTurn))),
						new BlackboardCondition ("targetOnRight", Operator.IS_EQUAL, true, Stops.IMMEDIATE_RESTART,
						// Turn left toward target
							new Action (() => Turn (turnPrecision))),
						new Action (() => Turn (-turnPrecision))
					)
				)
			);
		}

		private void UpdatePerception ()
		{
			Vector3 targetPos = TargetTransform ().position;
			Vector3 localPos = this.transform.InverseTransformPoint (targetPos);
			Vector3 heading = localPos.normalized;
			blackboard ["targetDistance"] = localPos.magnitude;
			blackboard ["targetInFront"] = heading.z > 0;
			blackboard ["targetOnRight"] = heading.x > 0;
			blackboard ["targetFarRight"] = heading.x > 0.08f;
			blackboard ["targetFarLeft"] = heading.x < -0.08f;

			blackboard ["targetOffCentre"] = Mathf.Abs (heading.x);
		}
	}
}