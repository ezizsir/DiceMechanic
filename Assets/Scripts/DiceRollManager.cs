using UnityEngine;

public class DiceRollManager : MonoBehaviour
{
    [Header("Dice Settings:")]
    [SerializeField] private DieRoller[] dice;
    private int rollsLeft = 0;


    // Button listener: starts all dice at once
    public void RollAllDice()
    {
        rollsLeft = dice.Length;

        for (int i = 0; i < rollsLeft; i++)
        {
            dice[i].RollButtonPressed();
        }
    }

}
