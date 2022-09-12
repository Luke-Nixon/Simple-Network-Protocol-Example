using static Network_Protocol_Library.Network_Settings;

namespace Network_Protocol_Library
{
    /// <summary>
    /// Used to send player information over the network
    /// </summary>
    public class Player
    {
        /// <summary>
        /// supported_Type that all serializeable types must have.
        /// </summary>
        public Supported_Type supported_Type = Supported_Type.player;
        public string UID;
        public string display_name;


        /// <summary>
        /// https://stackoverflow.com/questions/1387074/c-sharp-any-code-optimization-technique-for-overriding-equals/1387361
        ///  compares one player object to another and returns true if the UIDs and display_names match
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            Player other_player = obj as Player;
            return (other_player != null) && (other_player.GetType() == this.GetType() && other_player.UID == this.UID && other_player.display_name == this.display_name);
        }


        /// <summary>
        ///  https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-overriding-gethashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + this.display_name.GetHashCode();
                hash = hash * 23 + this.UID.GetHashCode();
                return hash;
            }
        }
    }
}