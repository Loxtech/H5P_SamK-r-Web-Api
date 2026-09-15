namespace H5P_Samkør_Web_Api.Models.DTOs;

public record TripOverviewResponse(
    IEnumerable<TripOverviewItem> Planned,
    IEnumerable<TripOverviewItem> Completed
);
