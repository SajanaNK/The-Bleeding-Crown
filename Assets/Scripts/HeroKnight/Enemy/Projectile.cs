using UnityEngine;

namespace HeroKnightSandbox.Enemy
{
    /// <summary>
    /// A simple runtime-spawned projectile. Flies in a straight line and checks proximity
    /// to its target each frame - dodgeable, consistent with how melee hit-detection
    /// elsewhere in this codebase is a plain distance check rather than a physics
    /// collider callback.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private static Sprite cachedSprite;

        private HeroKnightController target;
        private float direction;
        private float speed;
        private int damage;
        private float hitRadius;
        private float lifetime;
        private float age;

        public static Projectile Spawn(Vector3 position, float direction, HeroKnightController target, int damage,
            Sprite sprite = null, float speed = 6f, float hitRadius = 0.6f, float lifetime = 3f)
        {
            GameObject go = new GameObject("Projectile");
            go.transform.position = position;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            if (sprite != null)
            {
                // Arrow.png (Assets/FlexUnit/2DMedievalWeaponPack) points right by
                // default - flip it when firing left instead of relying on the
                // character-facing localScale.x convention used elsewhere, since this
                // sprite has no such existing convention of its own.
                sr.sprite = sprite;
                sr.flipX = direction < 0f;
            }
            else
            {
                sr.sprite = GetOrCreateSprite();
                sr.color = new Color(0.9f, 0.2f, 0.2f);
            }

            sr.sortingOrder = 10;

            Projectile projectile = go.AddComponent<Projectile>();
            projectile.target = target;
            projectile.direction = direction;
            projectile.damage = damage;
            projectile.speed = speed;
            projectile.hitRadius = hitRadius;
            projectile.lifetime = lifetime;
            return projectile;
        }

        private static Sprite GetOrCreateSprite()
        {
            if (cachedSprite != null)
            {
                return cachedSprite;
            }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            cachedSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 8f);
            return cachedSprite;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += new Vector3(direction * speed * Time.deltaTime, 0f, 0f);

            if (target != null && Vector2.Distance(transform.position, target.transform.position) <= hitRadius)
            {
                target.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}
