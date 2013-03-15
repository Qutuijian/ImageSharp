﻿// -----------------------------------------------------------------------
// <copyright file="PersistantDictionary.cs" company="James South">
//     Copyright (c) James South.
//     Dual licensed under the MIT or GPL Version 2 licenses.
// </copyright>
// -----------------------------------------------------------------------

namespace ImageProcessor.Web.Caching
{
    #region Using
    using System;
    using System.Collections.Generic;
    #endregion

    /// <summary>
    /// Represents a collection of keys and values whose operations are concurrent.
    /// </summary>
    internal sealed class PersistantDictionary : LockedDictionary<string, CachedImage>
    {
        #region Fields
        /// <summary>
        /// A new instance Initializes a new instance of the <see cref="T:ImageProcessor.Web.Caching.PersistantDictionary"/> class.
        /// initialized lazily.
        /// </summary>
        private static readonly Lazy<PersistantDictionary> Lazy =
                        new Lazy<PersistantDictionary>(() => new PersistantDictionary());

        /// <summary>
        /// The object to lock against.
        /// </summary>
        private static readonly object SyncRoot = new object();
        #endregion

        #region Constructors
        /// <summary>
        /// Prevents a default instance of the <see cref="T:ImageProcessor.Web.Caching.PersistantDictionary"/> class 
        /// from being created. 
        /// </summary>
        private PersistantDictionary()
        {
            this.LoadCache();
        }
        #endregion

        /// <summary>
        /// Gets the current instance of the <see cref="T:ImageProcessor.Web.Caching.PersistantDictionary"/> class.
        /// </summary>
        public static PersistantDictionary Instance
        {
            get
            {
                return Lazy.Value;
            }
        }

        #region Public
        /// <summary>
        /// Tries to remove the value associated with the specified key.
        /// </summary>
        /// <param name="key">
        /// The key of the item to remove.
        /// </param>
        /// <param name="value">
        /// The value to assign the returned value to.
        /// </param>
        /// <returns>
        /// true if the <see cref="PersistantDictionary"/> removes an element with 
        /// the specified key; otherwise, false.
        /// </returns>
        public bool TryRemove(string key, out CachedImage value)
        {
            // No CachedImage to remove.
            if (!this.ContainsKey(key))
            {
                value = default(CachedImage);
                return false;
            }

            // Remove the CachedImage.
            lock (SyncRoot)
            {
                value = this[key];
                this.Remove(key);

                this.SaveCache(key, value, true);

                return true;
            }
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary or returns the value if it exists.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="cachedImage">
        /// The cached image to add.
        /// </param>
        /// <returns>
        /// The value of the item to add or get.
        /// </returns>
        public new CachedImage Add(string key, CachedImage cachedImage)
        {
            lock (SyncRoot)
            {
                // Add the CachedImage.
                if (this.SaveCache(key, cachedImage, false))
                {
                    this[key] = cachedImage;
                }

                return cachedImage;
            }
        }
        #endregion

        /// <summary>
        /// Saves the in memory cache to the file-system.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="cachedImage">
        /// The cached Image.
        /// </param>
        /// <param name="remove">
        /// The remove.
        /// </param>
        /// <returns>
        /// true, if the dictionary is saved to the file-system; otherwise, false.
        /// </returns>
        private bool SaveCache(string key, CachedImage cachedImage, bool remove)
        {
            try
            {
                if (remove)
                {
                    return SQLContext.RemoveImage(key);
                }

                return SQLContext.AddImage(key, cachedImage);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Loads the cache file to populate the in memory cache.
        /// </summary>
        private void LoadCache()
        {
            lock (SyncRoot)
            {
                SQLContext.CreateDatabase();

                Dictionary<string, CachedImage> dictionary = SQLContext.GetImages();

                foreach (KeyValuePair<string, CachedImage> pair in dictionary)
                {
                    this.Add(pair);
                }
            }
        }
    }
}