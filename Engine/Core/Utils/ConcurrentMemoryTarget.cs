using System.Collections.Concurrent;
// 
// Copyright (c) 2004-2021 Jaroslaw Kowalski <jaak@jkowalski.net>, Kim Christensen, Julian Verdurmen
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

namespace NLog.Targets
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	/// <summary>
	/// Writes log messages to <see cref="Logs"/> in memory for programmatic retrieval.
	/// </summary>
	/// <seealso href="https://github.com/nlog/nlog/wiki/Memory-target">Documentation on NLog Wiki</seealso>
	/// <example>
	/// <p>
	/// To set up the target in the <a href="config.html">configuration file</a>, 
	/// use the following syntax:
	/// </p>
	/// <code lang="XML" source="examples/targets/Configuration File/Memory/NLog.config" />
	/// <p>
	/// This assumes just one target and a single rule. More configuration
	/// options are described <a href="config.html">here</a>.
	/// </p>
	/// <p>
	/// To set up the log target programmatically use code like this:
	/// </p>
	/// <code lang="C#" source="examples/targets/Configuration API/Memory/Simple/Example.cs" />
	/// </example>
	[Target("ConcurrentMemory")]
	public sealed class ConcurrentMemoryTarget : TargetWithLayout
	{
		readonly object lockObj = new object();
		readonly List<Tuple<LogLevel,string>> logs = new List<Tuple<LogLevel,string>>();

		public event EventHandler MessageLogged;


		public ConcurrentMemoryTarget()
		{
		}

		public ConcurrentMemoryTarget(string name) : this()
		{
			Name = name;
		}

		[DefaultValue(0)]
		public int MaxLogsCount { get; set; }


		public Tuple<LogLevel,string>[] Logs 
		{ 
			get { 
				lock (lockObj) 
				{ 
					return logs.ToArray();
				} 
			}
		}


		protected override void Write(LogEventInfo logEvent)
		{
			lock (lockObj)
			{
				if (MaxLogsCount > 0 && logs.Count >= MaxLogsCount)
				{
					logs.RemoveAt(0);
				}

				logs.Add( Tuple.Create( logEvent.Level, RenderLogEvent(Layout, logEvent) ) );

				MessageLogged?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}