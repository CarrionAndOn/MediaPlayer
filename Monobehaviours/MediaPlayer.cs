﻿using System;
using BoneLib;
using BoneLib.Notifications;
using MediaPlayer.Melon;
using MelonLoader;
using SLZ.SFX;
using TMPro;
using UnityEngine;
using Object = System.Object;

namespace MediaPlayer.Monobehaviours
{
    [RegisterTypeInIl2Cpp]
    public class MediaPlayer : MonoBehaviour
    {
        private AudioSource _audioSource;
        private ImpactSFX _impactSfx;
        private MeshRenderer _meshRenderer;
        private TextMeshPro _titleText;
        private TextMeshPro _authorText;
        private int _currentClipIndex;
        private float _pauseTime;

        private void Start()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            _impactSfx = gameObject.GetComponent<ImpactSFX>();
            _audioSource.outputAudioMixerGroup = Audio.MusicMixer;
            _impactSfx.outputMixer = Audio.SFXMixer;
            _meshRenderer = gameObject.transform.Find("Metadata/AlbumArt").GetComponent<MeshRenderer>();
            _titleText = gameObject.transform.Find("Metadata/Title").GetComponent<TextMeshPro>();
            _authorText = gameObject.transform.Find("Metadata/Artist").GetComponent<TextMeshPro>();
            if (Main.IsAndroid)
            {
                Destroy(_authorText.transform.gameObject);
            }
            PlayNextClip();
        }

        public void PlayPause()
        {
            if (_audioSource.isPlaying)
            {
                Pause();
            }
            else
            {
                Resume();
            }
        }

        public void Pause()
        {
            _pauseTime = _audioSource.time;
            _audioSource.Pause();
        }

        public void Resume()
        {
            _audioSource.Play();
            _audioSource.time = _pauseTime;
        }

        private void Update()
        {
            if (!_audioSource.isPlaying)
            {
                if (_currentClipIndex < Assets.AudioClips.Count)
                {
                    PlayNextClip();
                }
                else
                {
                    _currentClipIndex = 0;
                    PlayNextClip();
                }
            }
        }

        private void PlayNextClip()
        {
            if (_currentClipIndex < Assets.AudioClips.Count)
            {
                _audioSource.clip = Assets.AudioClips[_currentClipIndex];
                _audioSource.Play();
                // It's all broken as hell on Quest. Avoid taglib on quest, give PC the cool shit.
                if (!Main.IsAndroid)
                {
                    var icon = Assets.GrabCoverFromTags(_currentClipIndex);
                    if (icon == null)
                    {
                        icon = Assets.DummyIcon;
                    }
                    var author = Assets.GrabAuthorFromTags(_currentClipIndex);
                    var title = Assets.GrabTitleFromTags(_currentClipIndex);
                    UpdateStatus(icon, author, title);
                    _currentClipIndex++;
                    if (!Preferences.notificationsEnabled) return;
                    var notif = new Notification()
                    {
                        Title = "Now Playing:",
                        Message = $"{author} - {title}",
                        Type = NotificationType.CustomIcon,
                        IsPopup = true,
                        PopupLength = 3f,
                        ShowTitleOnPopup = true
                    };
                    Notifier.Send(notif, icon);
                }
                else
                {
                    var title = Assets.QuestGrabTitle(_currentClipIndex);
                    UpdateQuestStatus(title);
                    _currentClipIndex++;
                    if (!Preferences.notificationsEnabled) return;
                    var notif = new Notification()
                    {
                        Title = "Now Playing:",
                        Message = $"{title}",
                        Type = NotificationType.Information,
                        IsPopup = true,
                        PopupLength = 3f,
                        ShowTitleOnPopup = true
                    };
                    Notifier.Send(notif);
                }
            }
        }

        private void UpdateStatus(Texture icon, string author, string title)
        {
            _meshRenderer.material.mainTexture = icon;
            _authorText.text = author;
            _titleText.text = title;
        }

        private void UpdateQuestStatus(string title)
        {
            _titleText.text = title;
            _meshRenderer.material.mainTexture = Assets.DummyIcon;
        }
        
        public MediaPlayer(IntPtr ptr) : base(ptr) { }
    }
}