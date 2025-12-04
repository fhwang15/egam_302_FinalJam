using Mono.Cecil.Cil;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;


public enum BossState
{
    phaseOne,
    phaseTwo
}
public enum PhaseOne
{
    Detect,
    Smash,
    RoundAttack,
    CornShooting,
    Wind
}

public class BossManager : MonoBehaviour
{
    private GameObject BossCharacter;
    private Vector3 BossForward;
    public Vector3 PlayerCharacter;

    //List of transforms that will record the position of the stitches
    public List<Transform> StitchLocation;

    //Enum (State of Phase One and Two)
    BossState currentBossPhase = BossState.phaseOne;

    //Enum (States of Detect/AOE1/AOE2/AOE3)
    PhaseOne currentPhase = PhaseOne.Detect;

    //Health
    public float health;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BossCharacter = GetComponent<GameObject>();
        BossForward = BossCharacter.transform.forward;
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentPhase)
        {
            case PhaseOne.Detect:
                Detect();
                break;
            case PhaseOne.Smash:

                break;
            case PhaseOne.RoundAttack:

                break;

            case PhaseOne.CornShooting:
                break;
            case PhaseOne.Wind:

                break;
            default:

                break;
        }
    }

    public void Detect()
    {
        float thePlayerPositionSetsNextAttack = Vector3.Dot(BossForward, PlayerCharacter);

        if(thePlayerPositionSetsNextAttack < 0)
        {
            currentPhase = PhaseOne.RoundAttack;
        } 
        else if(thePlayerPositionSetsNextAttack < 0.5f)
        {
            currentPhase = PhaseOne.Smash;
        } 
        else if(thePlayerPositionSetsNextAttack > 0.5f)
        {
            currentPhase = PhaseOne.CornShooting;
        }

        //Dot Product for Where boss is looking / Character position
        
        //If the Dot Product is less than some amount, then it will be smach,

        //if the dot product is large, CornShooting

        //if the dot product is negative == Round Attack

        // Wind idk random ig
    }

    public void Smash()
    {

    }


    public IEnumerator CoolDown()
    {
        yield return new WaitForSeconds(2f);
    }

}