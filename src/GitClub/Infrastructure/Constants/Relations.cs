// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GitClub.Infrastructure.Constants
{
    /// <summary>
    /// Relations in the Application.
    /// </summary>
    public static class Relations
    {
        /// <summary>
        /// Admin.
        /// </summary>
        public static readonly string Admin = "admin";
        
        /// <summary>
        /// Admin.
        /// </summary>
        public static readonly string Maintainer = "maintainer";
        
        /// <summary>
        /// Owner.
        /// </summary>
        public static readonly string Owner = "owner";
        
        /// <summary>
        /// Triager.
        /// </summary>
        public static readonly string Triager = "triager";
        
        /// <summary>
        /// Writer.
        /// </summary>
        public static readonly string Writer = "writer";

        /// <summary>
        /// Member.
        /// </summary>
        public static readonly string Member = "member";
        
        /// <summary>
        /// Creator.
        /// </summary>
        public static readonly string Creator = "creator";

        /// <summary>
        /// RepoAdmin.
        /// </summary>
        public static readonly string RepoAdmin = "repo_admin";

        /// <summary>
        /// RepoReader.
        /// </summary>
        public static readonly string RepoReader = "repo_reader";

        /// <summary>
        /// RepoWriter.
        /// </summary>
        public static readonly string RepoWriter = "repo_writer";
    }
}
