// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Clients.AgentBuilder;

/// <summary> Controls how bootstrapping of Agent Builder resources is handled. </summary>
public enum BootstrapMethod
{
	/// <summary> No bootstrap should occur. </summary>
	None,

	/// <summary> Bootstrap silently, ignoring any failures. </summary>
	Silent,

	/// <summary> Bootstrap and throw exceptions on failure. </summary>
	Failure
}
