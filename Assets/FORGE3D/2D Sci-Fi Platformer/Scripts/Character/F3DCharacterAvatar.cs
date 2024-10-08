using System;
using UnityEngine;
using System.Collections;

public class F3DCharacterAvatar : MonoBehaviour
{
    public bool RandomizeAtStart;

    public int CharacterId;
    public SpriteRenderer Head;
    public SpriteRenderer Body;

    //
    private F3DWeaponController _weaponController;

    //
    [Serializable]
    public class CharacterArmature
    {
        public Sprite Head;
        public Sprite Body;
        public Sprite Hand1;
        public Sprite Hand2;
        public Sprite Hand3;
        public Sprite Hand4;
    }

    public CharacterArmature[] Characters;

    //
    private void Awake()
    {
        _weaponController = GetComponent<F3DWeaponController>();
        if (RandomizeAtStart) CharacterId = UnityEngine.Random.Range(0, 6);
        SwitchCharacter(CharacterId);
    }

    private void SwitchCharacter(int id)
    {
        if (Head == null) return;
        if (Body == null) return;
        if (Characters == null || id >= Characters.Length || id < 0) return;
        Head.sprite = Characters[CharacterId].Head;
        Body.sprite = Characters[CharacterId].Body;
        _weaponController.UpdateCharacterHands(Characters[CharacterId]);
    }

    private void Update()
    {
        if (Head == null) return;
        if (Body == null) return;
     
        // Debug
        DebugSwitchCharacter();
    }

    // DEBUG 
    private void DebugSwitchCharacter()
    {
        if(Input.GetKeyDown(KeyCode.Alpha8))
        {
            CharacterId++;
            if (CharacterId > 5)
                CharacterId = 0;
            SwitchCharacter(CharacterId);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            CharacterId--;
            if (CharacterId < 0)
                CharacterId = 5;
            SwitchCharacter(CharacterId);
        }
         
        /*
        if (Input.GetKeyDown(KeyCode.F1))
        {
            CharacterId = 0;
            SwitchCharacter(CharacterId);
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            CharacterId = 1;
            SwitchCharacter(CharacterId);
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            CharacterId = 2;
            SwitchCharacter(CharacterId);
        }
        else if (Input.GetKeyDown(KeyCode.F4))
        {
            CharacterId = 3;
            SwitchCharacter(CharacterId);
        }
        else if (Input.GetKeyDown(KeyCode.F5))
        {
            CharacterId = 4;
            SwitchCharacter(CharacterId);
        }
        else if (Input.GetKeyDown(KeyCode.F6))
        {
            CharacterId = 5;
            SwitchCharacter(CharacterId);
        }*/
    }
}