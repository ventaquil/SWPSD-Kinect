using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinect
{
    public class Track
    {
        public readonly string Title;

        public readonly string Path;

        public Track(string path)
        {
            Path = path;

            Title = GetTitle();
        }

        private string GetTitle()
        {
            string title = System.IO.Path.GetFileNameWithoutExtension(Path);

            return title;
        }
    }
}
