﻿using System;

namespace BarionClientLibrary
{
    public class BarionSettings
    {
        public Guid POSKey { get; set; }

        public Uri BaseUrl { get; set; }

        public string Payee { get; set; }
    }
}