using UnityEngine;

namespace PathBerserker2d.Demo
{
    /// <summary>
    /// An example breakable wall. NavAgent with Pickaxe script can destroy it.  
    /// </summary>
    public class Breakable : MonoBehaviour
    {
        [SerializeField]
        Material breakMat = null;
        [SerializeField]
        float health = 1;

        int shaderProgressProp;

        private void Awake()
        {
            shaderProgressProp = breakMat.shader.GetPropertyNameId(3);
            breakMat.SetFloat(shaderProgressProp, 1 - health);
        }

        public bool Break()
        {
            health -= Time.deltaTime;
            breakMat.SetFloat(shaderProgressProp, 1 -health);

            if (health <= 0)
            {
                Destroy(this.gameObject);
                return true;
            }
            return false;
        }
    }
}
