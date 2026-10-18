using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Coffee/Customer")]
public class CustomerSO : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite neutral, happy, angry;
    public float patienceSeconds = 30f;
    public float tipMultiplier = 1f;
    [TextArea] public string personality;             // fed to the LLM order generator later
}
