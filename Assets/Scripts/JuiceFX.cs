using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Win/lose juice: confetti burst at the Door on win, camera shake plus a
/// short red tint on the hero on lose. Component is created at runtime by
/// LevelRunner, subscribes to the GameManager events.
/// </summary>
public class JuiceFX : MonoBehaviour
{
    void OnEnable()
    {
        GameManager.EnsureInstance();
        GameManager.Instance.OnWon += OnWon;
        GameManager.Instance.OnLost += OnLost;
        GameManager.Instance.OnLevelLoaded += OnLevelLoaded;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWon -= OnWon;
            GameManager.Instance.OnLost -= OnLost;
            GameManager.Instance.OnLevelLoaded -= OnLevelLoaded;
        }
    }

    void OnWon()
    {
        // "Win" sound is already played by RopeCutGoal; add confetti only.
        var goal = GameObject.Find("Goal");
        if (goal != null) SpawnConfetti(goal.transform.position + Vector3.up * 0.6f);
    }

    void OnLost()
    {
        CameraShake.Trigger(0.3f, 0.2f);
        StartCoroutine(TintHero());
    }

    void OnLevelLoaded()
    {
        GetComponentRestore();
    }

    void GetComponentRestore()
    {
        StopAllCoroutines();
        var hero = GameObject.FindWithTag("Hero");
        if (hero != null)
        {
            var sr = hero.GetComponent<SpriteRenderer>();
            if (sr != null && heroOriginalStored)
                sr.color = heroOriginal;
            heroOriginalStored = false;
        }
    }

    IEnumerator TintHero()
    {
        var hero = GameObject.FindWithTag("Hero");
        if (hero == null) yield break;
        var sr = hero.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        if (!heroOriginalStored)
        {
            heroOriginal = sr.color;
            heroOriginalStored = true;
        }
        sr.color = new Color(1f, 0.35f, 0.3f);
        yield return new WaitForSeconds(0.3f);
        sr.color = heroOriginal;
        heroOriginalStored = false;
    }

    void SpawnConfetti(Vector3 at)
    {
        var go = new GameObject("~Confetti");
        var loaderGo = FindFirstObjectByType<LevelLoader>();
        if (loaderGo == null) return; // not in a game scene
        go.transform.SetParent(loaderGo.transform, false);
        go.transform.position = at;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 1.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.gravityModifier = 0.4f;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.98f, 0.4f, 0.4f), 0f),
                new GradientColorKey(new Color(0.98f, 0.86f, 0.15f), 0.25f),
                new GradientColorKey(new Color(0.3f, 0.85f, 0.6f), 0.5f),
                new GradientColorKey(new Color(0.35f, 0.7f, 1f), 0.75f),
                new GradientColorKey(new Color(0.9f, 0.5f, 0.95f), 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        main.startColor = new ParticleSystem.MinMaxGradient(gradient);

        var burst = ps.emission;
        burst.rateOverTime = 0f;
        burst.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.6f, 0.2f, 0.2f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        var mat = new Material(Shader.Find("Sprites/Default"));
        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 20;

        ps.Play();
        Destroy(go, 3f);
    }

    Color heroOriginal;
    bool heroOriginalStored;
}
