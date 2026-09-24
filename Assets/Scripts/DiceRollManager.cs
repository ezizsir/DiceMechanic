using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DiceRollManager : MonoBehaviour
{
    [Header("Dice Settings:")]
    [SerializeField] private DieRoller[] dice;          // all 5 slots, index 0-4
    [SerializeField] private int[] shuffleCounts = { 8, 12, 16, 20, 24 }; // die i shuffles this many times

    [Header("Timing:")]
    [SerializeField] private float raiseDelay = 0.1f;    // stagger between dice lifting up

    [Header("Add Die:")]
    [SerializeField] private GameObject addDieButton;    // Add Die button (starts inactive)

    [Header("Score Display:")]
    [SerializeField] private TextMeshProUGUI scoreText;  // TOTAL display
    [SerializeField] private TextMeshProUGUI multiplierText; // multiplier title + value
    [SerializeField] private TextMeshProUGUI payoutText; // TOTAL x multiplier
    [SerializeField] private Color scoreColor = Color.white;
    [SerializeField] private Color multiplierTitleColor = Color.yellow;
    [SerializeField] private Color payoutColor = Color.green;

    [Header("Hand Multipliers:")]
    [SerializeField] private HandRule[] handRules = new HandRule[]
    {
        new HandRule("Five of a kind", 10f),
        new HandRule("Full House", 3.5f),
        new HandRule("Four of a kind", 5f),
        new HandRule("Straight", 3f),
        new HandRule("Three of a kind", 2.5f),
        new HandRule("Two pairs", 2f),
        new HandRule("One pair", 1.5f),
        new HandRule("High Roller", 1f),
    };

    // Unity can't serialize a Dictionary, so the editable list above is converted
    // into one at startup. EvaluateHand looks multipliers up here by title.
    private Dictionary<string, float> multiplierLookup;

    private int activeDice = 0;   // how many dice currently in play (starts at 1)
    private bool awaitingClicks = false;

    private void Awake()
    {
        // Build the title -> multiplier lookup from the Inspector list
        multiplierLookup = new Dictionary<string, float>();
        foreach (HandRule rule in handRules)
        {
            multiplierLookup[rule.title] = rule.multiplier;
        }
    }

    private void Start()
    {
        // Start with only one die; the rest are hidden and inactive
        for (int i = 1; i < dice.Length; i++)
        {
            dice[i].gameObject.SetActive(false);
        }
        activeDice = 1;
    }

    // Roll button listener
    public void RollAllDice()
    {
        StartCoroutine(RollSequence());
    }

    private IEnumerator RollSequence()
    {
        awaitingClicks = false;

        // 1. All UNLOCKED dice rise up together (staggered slightly so it reads as a group)
        for (int i = 0; i < activeDice; i++)
        {
            if (dice[i].IsLocked) continue; // locked die: no rise, no shuffle, keeps its face
            dice[i].ResetLayers();
            dice[i].RaiseUp();
            yield return new WaitForSeconds(raiseDelay);
        }
        yield return new WaitForSeconds(0.3f); // let the rise finish

        // 2. All unlocked dice shuffle at the same time, different counts so they drop one by one
        int done = 0;
        int toRoll = 0;
        for (int i = 0; i < activeDice; i++)
        {
            if (dice[i].IsLocked) continue;
            toRoll++;
            DieRoller die = dice[i];
            die.OnRollEnd += OnDieFinished;
            die.BeginShuffle(shuffleCounts[i]);
        }

        void OnDieFinished(DieRoller d)
        {
            d.OnRollEnd -= OnDieFinished;
            done++;
        }

        // wait until every unlocked die has finished shuffling and dropped
        yield return new WaitUntil(() => done >= toRoll);

        // 3. Report results
        ReportResults();

        // 4. Enable locking for this turn
        awaitingClicks = true;
        for (int i = 0; i < activeDice; i++)
        {
            dice[i].CanLock = true;
        }
    }

    private void ReportResults()
    {
        List<int> values = new List<int>();
        int total = 0;

        for (int i = 0; i < activeDice; i++)
        {
            int v = dice[i].FaceValue;
            values.Add(v);
            total += v;
            Debug.Log($"Die {i + 1}: {v}");
        }

        (string title, float multiplier) = EvaluateHand(values);

        float payout = total * multiplier;

        Debug.Log($"TOTAL: {total}");
        Debug.Log($"{title} x{multiplier}");
        Debug.Log($"PAYOUT: {payout}");

        // In-game display
        if (scoreText != null)
        {
            scoreText.text = $"TOTAL: {total}";
            scoreText.color = scoreColor;
        }
        if (multiplierText != null)
        {
            multiplierText.text = $"{title}  x{multiplier}";
            multiplierText.color = multiplierTitleColor;
        }
        if (payoutText != null)
        {
            payoutText.text = $"PAYOUT: {payout}";
            payoutText.color = payoutColor;
        }
    }

    // One row in the Inspector: hand title + its multiplier value
    [Serializable]
    public class HandRule
    {
        public string title;      // e.g. "Full House"
        public float multiplier;  // e.g. 3.5

        public HandRule(string title, float multiplier)
        {
            this.title = title;
            this.multiplier = multiplier;
        }
    }

    private (string, float) EvaluateHand(List<int> v)
    {
        // count how many times each face appears
        int[] counts = new int[7];
        foreach (int x in v) counts[x]++;

        bool hasPair = false, hasTwoPair = false, hasThree = false, hasFour = false, hasFive = false;
        foreach (int c in counts)
        {
            if (c == 2) { if (hasPair) hasTwoPair = true; else hasPair = true; }
            if (c == 3) hasThree = true;
            if (c == 4) hasFour = true;
            if (c == 5) hasFive = true;
        }

        // Which hand matched? Order matters: strongest hand first.
        string matched;
        if (hasFive) matched = "Five of a kind";
        else if (hasFour) matched = "Four of a kind";
        else if (hasThree && hasPair) matched = "Full House";
        else if (counts[1] >= 1 && counts[2] >= 1 && counts[3] >= 1 && counts[4] >= 1 && counts[5] >= 1 && counts[6] >= 1 && v.Count == 5) matched = "Straight";
        else if (hasThree) matched = "Three of a kind";
        else if (hasTwoPair) matched = "Two pairs";
        else if (hasPair) matched = "One pair";
        else matched = "High Roller";

        // Multiplier comes from the Inspector-configured dictionary
        return (matched, multiplierLookup.TryGetValue(matched, out float m) ? m : 1f);
    }

    // Add Die button listener: adds the next slot, max 5 total
    public void AddDie()
    {
        if (activeDice >= dice.Length) return;

        dice[activeDice].gameObject.SetActive(true);
        activeDice++;

        if (activeDice >= dice.Length)
        {
            if (addDieButton != null) addDieButton.SetActive(false); // fade the whole button out
        }
    }
}
