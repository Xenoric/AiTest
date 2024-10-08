using UnityEngine;
using System.Collections;

public class F3DAssault : F3DGenericWeapon
{
    private void DespawnFXSocketObjects()
    {
        var fxSocketObjects = FXSocket.GetComponentsInChildren<Animator>();
        if (fxSocketObjects == null) return;
        for (var i = 0; i < fxSocketObjects.Length; i++)
            if (fxSocketObjects[i] != null)
            {
                F3DSpawner.Despawn(fxSocketObjects[i].gameObject);
                Debug.Log("Despawn " + fxSocketObjects[i].gameObject.name);
            }
    }
}