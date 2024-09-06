using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class PoissonSampler
{	
	public static List<SamplePoint> GeneratePoints3D(float radius, Vector2 sampleRegionSize, float projectionStart, int seed, string[] tagFilters = null, float? maxProjectionDistance = null, bool invertLayerMask = true, int numSamplesBeforeRejection = 30, Vector2? sampleStart = null, Vector3? projectionDirection = null, params ICandidateFilter[] filters)
    {
		List<Vector2> points = GeneratePoints(radius, sampleRegionSize, seed, numSamplesBeforeRejection, sampleStart, filters);
		List<SamplePoint> targets = new List<SamplePoint>();
		
		Vector3 direction = projectionDirection.HasValue ? projectionDirection.Value : Vector3.down;
		float projectionDistance = maxProjectionDistance.HasValue ? maxProjectionDistance.Value : 10f;		

		if (points != null && points.Count > 0)
		{			
			for (int i = 0; i < points.Count; i++)
            {
				RaycastHit hit;
				Vector3 p = new Vector3(points[i].x, projectionStart, points[i].y);
				
				if (Physics.Raycast(p, Vector3.down, out hit, projectionDistance))
				{															
					if(tagFilters != null && tagFilters.Length > 0)
                    {
						if(!tagFilters.Any(tf => tf.Contains(hit.transform.tag)))
                        {
							targets.Add(new SamplePoint(hit.point, hit.normal));
                        }
                    }

					else
                    {
						targets.Add(new SamplePoint(hit.point, hit.normal));
					}					
				}
				
				else
				{										
					continue;
				}
			}
		}

		return targets;
    }

	public static List<Vector2> GeneratePoints(float radius, Vector2 sampleRegionSize, int seed, int numSamplesBeforeRejection = 30, Vector2? sampleStart = null, params ICandidateFilter[] filters)
	{
		float cellSize = radius / Mathf.Sqrt(2);		
		
		Random.InitState(seed);

		int[,] grid = new int[Mathf.CeilToInt(sampleRegionSize.x / cellSize), Mathf.CeilToInt(sampleRegionSize.y / cellSize)];
		List<Vector2> points = new List<Vector2>();
		List<Vector2> spawnPoints = new List<Vector2>();		
		Vector2 startPoint = sampleRegionSize / 2;

		if(sampleStart.HasValue)
        {
			if((sampleStart.Value.x >= 0 && sampleStart.Value.x <= sampleRegionSize.x) && (sampleStart.Value.y >= 0 && sampleStart.Value.y <= sampleRegionSize.y))
            {
				startPoint = sampleStart.Value;
			}
        }

		spawnPoints.Add(startPoint);

		while (spawnPoints.Count > 0)
		{
			int spawnIndex = Random.Range(0, spawnPoints.Count);
			Vector2 spawnCentre = spawnPoints[spawnIndex];
									
			bool candidateAccepted = false;

			for (int i = 0; i < numSamplesBeforeRejection; i++)
			{
				float angle = Random.value * Mathf.PI * 2;
				
				Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				Vector2 candidate = spawnCentre + dir * Random.Range(radius, 2 * radius);
				bool isValid = false;

				if (filters == null || filters.Length <= 0 || !filters.Any(f => f.type == FilterType.Area))
				{
					isValid = IsValid(candidate, sampleRegionSize, cellSize, radius, points, grid);

				}

				else
                {
					isValid = IsValidWithFilters(candidate, sampleRegionSize, cellSize, radius, points, grid, filters);
				}

				if (isValid)
				{
					points.Add(candidate);
					spawnPoints.Add(candidate);
					
					grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count;
					
					candidateAccepted = true;
					
					break;
				}
			}
			if (!candidateAccepted)
			{
				spawnPoints.RemoveAt(spawnIndex);
			}

		}

		if (filters == null || filters.Length <= 0 || !filters.Any(f => f.type == FilterType.Post))
		{			
			return points;
		}

		else
        {			
			List<Vector2> filteredPoints = new List<Vector2>();
			List<ICandidateFilter> postFilters = filters.Where(f => f.type == FilterType.Post).ToList();
			
			if (postFilters != null && postFilters.Count > 0)
			{
                foreach (Vector2 point in points)
                {
                    foreach (ICandidateFilter filter in postFilters)
                    {
                        if (filter.Filter(point) && filteredPoints.Contains(point))
                        {
                            continue;
                        }

						else
                        {
                            if (filter.Filter(point))
                            {
                                filteredPoints.Add(point);
                            }
                        }
                    }
                }
            }

			return filteredPoints;
        }
	}

	private static bool IsValid(Vector2 candidate, Vector2 sampleRegionSize, float cellSize, float radius, List<Vector2> points, int[,] grid)
	{				
		if (candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y)
		{
			int cellX = (int)(candidate.x / cellSize);
			int cellY = (int)(candidate.y / cellSize);

			int searchStartX = Mathf.Max(0, cellX - 2);
			int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);

			int searchStartY = Mathf.Max(0, cellY - 2);
			int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

			for (int x = searchStartX; x <= searchEndX; x++)
			{
				for (int y = searchStartY; y <= searchEndY; y++)
				{
					int pointIndex = grid[x, y] - 1;

					if (pointIndex != -1)
					{
						float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;

						if (sqrDst < radius * radius)
						{
							return false;
						}
					}
				}
			}

			return true;
		}		

		return false;
	}
	
	private static bool IsValidWithFilters(Vector2 candidate, Vector2 sampleRegionSize, float cellSize, float radius, List<Vector2> points, int[,] grid, params ICandidateFilter[] filters)
	{
		if (filters != null && filters.Length > 0)
		{
			if (filters.Any(f => f.type == FilterType.Area))
			{
				ICandidateFilter areaFilter = filters.FirstOrDefault(f => f.type == FilterType.Area);

				if (areaFilter != null)
				{
					// Default rect
					if (areaFilter.Filter(candidate))
					{
						int cellX = (int)(candidate.x / cellSize);
						int cellY = (int)(candidate.y / cellSize);

						int searchStartX = Mathf.Max(0, cellX - 2);
						int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);

						int searchStartY = Mathf.Max(0, cellY - 2);
						int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

						for (int x = searchStartX; x <= searchEndX; x++)
						{
							for (int y = searchStartY; y <= searchEndY; y++)
							{
								int pointIndex = grid[x, y] - 1;

								if (pointIndex != -1)
								{
									float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;

									if (sqrDst < radius * radius)
									{
										return false;
									}
								}
							}
						}

						return true;
					}
				}
			}
		}

		return false;
	}	
}