﻿#if PRERELEASE
using SLZ.Bonelab;
using SLZ.Rig;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BoneLib;
using UnityEngine;

namespace MediaPlayer.Notifications
{
    // most of this comes from Fusion, with permission to use it here

    /// <summary>
    /// The basic types of notifications that can be sent.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// Used to inform the user.
        /// </summary>
        Information = 0,

        /// <summary>
        /// Used when the user should be notified of a potential issue.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Used when the user or program attempts a task and it fails.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Used when the user or program performs a task and it suceeds.
        /// </summary>
        Success = 3,

        /// <summary>
        /// Lets the user specify an customIcon
        /// </summary>
        CustomIcon = 4
    }


    /// <summary>
    /// The class used to supply text in a notification.
    /// </summary>
    public struct NotificationText
    {
        /// <summary>
        /// The text.
        /// </summary>
        public string Text;

        /// <summary>
        /// The color of the text.
        /// </summary>
        public Color Color;

        /// <summary>
        /// Should rich text be allowed?
        /// </summary>
        public bool RichText;

        public NotificationText(string text) : this(text, Color.white) { }

        public NotificationText(string text, Color color, bool richText = false)
        {
            if (!richText)
            {
                Regex rich = new(@"<[^>]*>");
                text = rich.Replace(text, string.Empty);
            }

            this.Text = text;
            this.Color = color;
            this.RichText = richText;
        }

        public static implicit operator NotificationText(string text) => new NotificationText(text);
    }


    /// <summary>
    /// The class used for sending notifications to the player. No constructors, provide your own information.
    /// </summary>
    public class Notification
    {
        // Text settings
        /// <summary>
        /// The title of the notification.
        /// </summary>
        public NotificationText Title;

        /// <summary>
        /// The main body of the notification.
        /// </summary>
        public NotificationText Message;

        // Popup settings
        /// <summary>
        /// Should the title be used on the popup? (If false, it shows "New Notification".)
        /// </summary>
        public bool ShowTitleOnPopup = false;

        /// <summary>
        /// How long the notification will be up.
        /// </summary>
        public float PopupLength = 2f;

        /// <summary>
        /// The type of notification this is.
        /// </summary>
        public NotificationType Type = NotificationType.Information;

        /// <summary>
        /// The customIcon to use. Only used if <see cref="Type"/> is <see cref="NotificationType.CustomIcon"/>.
        /// </summary>
        public Texture2D CustomIcon = null;
    }

    public static class Notifier
    {
        private static readonly Queue<Notification> QueuedNotifications = new();

        private static bool _hasEnabledTutorialRig = false;

        /// <summary>
        /// Sends a notification to the player.
        /// </summary>
        /// <param name="notification">The notification</param>
        public static void Send(Notification notification)
        {
            QueueNotification(notification);
        }

        private static void QueueNotification(Notification notification)
        {
            QueuedNotifications.Enqueue(notification);
        }

        private static void DequeueNotification()
        {
            Notification notification = QueuedNotifications.Dequeue();

            // Show to the player
            RigManager rm = Player.rigManager;

            if (rm != null)
            {
                TutorialRig tutorialRig = rm.tutorialRig;
                HeadTitles headTitles = tutorialRig.headTitles;

                EnableTutorialRig();

                string incomingTitle = "New Notification";

                if (notification.ShowTitleOnPopup)
                    incomingTitle = notification.Title.Text;

                string incomingSubTitle = notification.Message.Text;

                Texture2D incomingTexture = notification.Type switch
                {
                    NotificationType.Warning => Assets.DummyIcon,
                    NotificationType.Error => Assets.DummyIcon,
                    NotificationType.Success => Assets.DummyIcon,
                    NotificationType.Information => Assets.DummyIcon,
                    NotificationType.CustomIcon => notification.CustomIcon,
                    _ => Assets.DummyIcon
                };
                Sprite incomingSprite = Sprite.Create(incomingTexture, new Rect(0.0f, 0.0f, incomingTexture.width, incomingTexture.height), new Vector2(0.5f, 0.5f), 100.0f);

                float holdTime = notification.PopupLength;

                headTitles.CUSTOMDISPLAY(incomingTitle, incomingSubTitle, incomingSprite, holdTime);
                headTitles.sr_element.sprite = incomingSprite;
            }
        }

        private static void EnableTutorialRig()
        {
            RigManager rm = Player.rigManager;

            if (rm != null)
            {
                TutorialRig tutorialRig = rm.tutorialRig;
                HeadTitles headTitles = tutorialRig.headTitles;

                // Make sure the tutorial rig/head titles are enabled
                tutorialRig.gameObject.SetActive(true);
                headTitles.gameObject.SetActive(true);
            }
        }

        private static bool IsPlayingNotification()
        {
            RigManager rm = Player.rigManager;

            if (rm != null)
            {
                TutorialRig tutorialRig = rm.tutorialRig;
                HeadTitles headTitles = tutorialRig.headTitles;

                return headTitles.headFollower.gameObject.activeInHierarchy;
            }

            return false;
        }



        internal static void OnUpdate()
        {
            // Make sure we aren't loading so we can dequeue existing notifications
            if (QueuedNotifications.Count > 0 && !HelperMethods.IsLoading() && Player.rigManager != null)
            {
                // Enable the tutorial rig a frame before showing notifs
                if (!_hasEnabledTutorialRig)
                {
                    EnableTutorialRig();
                    _hasEnabledTutorialRig = true;
                }
                // Dequeue notifications
                else if (QueuedNotifications.Count > 0 && !IsPlayingNotification())
                {
                    DequeueNotification();
                }
            }
            else
            {
                _hasEnabledTutorialRig = false;
            }
        }
    }
}
#endif