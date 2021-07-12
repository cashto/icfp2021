## Problem description

Inspired by the Japanese gameshow game '[Brain Wall](https://en.wikipedia.org/wiki/Brain_Wall)': given a graph, fit it inside 
a designated target polygon. Graph edges can stretch to a certain limit, but graph vertexes must be on integer coordinates. The 
closer that solution vertexes approach the corners of the polygon target, the more points are awarded.

## Approach

Similar to last year, I have a [ASP.Net WebApi Core](https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-5.0) 
standalone server running on localhost, written in C#, serving an HTML/Javascript webpage which sends commands back 
to the localhost server via XMLHttpRequest.  The localhost server saves / loads solutions in progress and submits
them to the contest server.

Problems were mostly solved manually, with the occasional assistance of search tools on the localhost. I made no attempt
to take advantages of any bonuses.

A slightly modified version of [the UI can be found here](https://cashto.github.io/icfp2021/index.html). This version
strips out communication with the localhost server. Since validation/scoring was done on the localhost server, all edges are 
currently colored green. Edges that are too long/too short were colored yellow-red based on how bad they were
stretched, and purple if the edge was the right length but otherwise fell outside the target polygon area.

Usage:

* Click on a vertex to select it.
* Ctrl-click on a vertex to toggle its selection state.
* Drag with the right mouse button to move selected vertexes around.
* Mark the "rubberband" checkbox to automatically drag neighboring nodes.
* Use controls at the top to move between problems.
* Click reload to discard changes and go back to the original solution.
* Click reset to see the original problem.

Other controls:

* Selected vertex / All vertices to corner moves nodes to the closest corner, if distance 
to that corner is less than 20 units.  This was written about six hours before the end of the contest, 
and then hardly ever used.
* Pin corners: when rubberband is enabled, vertexes that are on corners are fixed into position.
* Corner gravity: when rubberband is enabled, try to pull nodes to the closest corner. Didn't work very well, never used.
* Greyed-out controls were performed on the localhost server:
    * Brute force: "the first, last, and only algorithm you'll ever need".
    * Incremental brute force: like brute force, but only considers moving selected nodes.  Occasionally useful 
      for automatically finding a legal position for two-three vertexes lying outside the polygon.
    * Optimize: given a valid solution, improve the solution by adjusting nodes.  Would often improve invalid solutions also.
    * Refine: moves vertexes around in an attempt to minimize the number of invalid "yellow" edges. Didn't work very well.
    * Assign corners: a variant of brute-force solution useful for finding exact solutions to problems where every corner of
      the polygon has a graph vertex.

## Results

* Lightning round, 60 of 78 problems solved, rank somewhere between 40-50 of ~110 teams.
* Standard round, 97 of 132 problems solved, rank somewhere between 60-70 of ~160 teams.

I didn't have time to solve every problem, so I priortized problems with high 'minimal dislikes' (ie, no team had
solved it perfectly or even near-perfectly).  There were quite a few problems when a trivial solution could have 
been made in minutes, but it wouldn't have scored more than a handful of points, so it wasn't work the time.
