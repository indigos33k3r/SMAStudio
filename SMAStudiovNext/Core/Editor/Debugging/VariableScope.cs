﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core.Editor.Debugging
{
    /// <summary>
    /// Contains details pertaining to a variable scope in the current
    /// debugging session.
    /// </summary>
    public class VariableScope
    {
        /// <summary>
        /// Gets a numeric ID that can be used in future operations
        /// relating to this scope.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Gets a name that describes the variable scope.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Initializes a new instance of the VariableScope class with
        /// the given ID and name.
        /// </summary>
        /// <param name="id">The variable scope's ID.</param>
        /// <param name="name">The variable scope's name.</param>
        public VariableScope(int id, string name)
        {
            this.Id = id;
            this.Name = name;
        }
    }
}
