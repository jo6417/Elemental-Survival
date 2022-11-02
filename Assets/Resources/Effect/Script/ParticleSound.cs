using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using DarkTonic.MasterAudio;
using System;
using System.Linq;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleSound : MonoBehaviour
{
    private ParticleSystem _parentParticleSystem;

    public string BornSound;

    public string DieSound;

    // private ExplosionPhysicsForceEffect _explosionPhysicsForceEffect;
    private IDictionary<uint, ParticleSystem.Particle> _trackedParticles = new Dictionary<uint, ParticleSystem.Particle>();

    void Start()
    {
        _parentParticleSystem = this.GetComponent<ParticleSystem>();
        if (_parentParticleSystem == null)
            Debug.LogError("Missing ParticleSystem!", this);

        // _explosionPhysicsForceEffect = this.GetComponent<ExplosionPhysicsForceEffect>();
    }

    void Update()
    {
        var liveParticles = new ParticleSystem.Particle[_parentParticleSystem.particleCount];
        _parentParticleSystem.GetParticles(liveParticles);

        var particleDelta = GetParticleDelta(liveParticles);

        foreach (var particleAdded in particleDelta.Added)
        {
            // Messenger.Broadcast(MessengerEventIds.PlaySoundAtVector3, new PlaySoundAtVector3MessengerEvent(BornSound, particleAdded.position, delayBasedOnDistanceToListener: true));
        }

        foreach (var particleRemoved in particleDelta.Removed)
        {
            // Messenger.Broadcast(MessengerEventIds.PlaySoundAtVector3, new PlaySoundAtVector3MessengerEvent(DieSound, particleRemoved.position, delayBasedOnDistanceToListener: true));
        }
    }

    private ParticleDelta GetParticleDelta(ParticleSystem.Particle[] liveParticles)
    {
        var deltaResult = new ParticleDelta();

        foreach (var activeParticle in liveParticles)
        {
            ParticleSystem.Particle foundParticle;
            if (_trackedParticles.TryGetValue(activeParticle.randomSeed, out foundParticle))
            {
                _trackedParticles[activeParticle.randomSeed] = activeParticle;
            }
            else
            {
                // 새로 생긴 파티클을 리스트에 추가
                deltaResult.Added.Add(activeParticle);
                _trackedParticles.Add(activeParticle.randomSeed, activeParticle);
            }
        }

        var updatedParticleAsDictionary = liveParticles.ToDictionary(x => x.randomSeed, x => x);
        var dictionaryKeysAsList = _trackedParticles.Keys.ToList();

        foreach (var dictionaryKey in dictionaryKeysAsList)
        {
            if (updatedParticleAsDictionary.ContainsKey(dictionaryKey) == false)
            {
                // 사라진 파티클을 리스트에 추가
                deltaResult.Removed.Add(_trackedParticles[dictionaryKey]);
                _trackedParticles.Remove(dictionaryKey);
            }
        }

        return deltaResult;
    }

    private class ParticleDelta
    {
        public IList<ParticleSystem.Particle> Added { get; set; } = new List<ParticleSystem.Particle>();
        public IList<ParticleSystem.Particle> Removed { get; set; } = new List<ParticleSystem.Particle>();
    }
}