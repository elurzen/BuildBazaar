DELIMITER $$
CREATE DEFINER=`root`@`%` PROCEDURE `sp_SearchBuilds`(IN `p_gameID` INT, IN `p_buildName` VARCHAR(255), IN `p_author` VARCHAR(255), IN `p_classID` INT, IN `p_tag1` VARCHAR(100), IN `p_tag2` VARCHAR(100), IN `p_tag3` VARCHAR(100), IN `p_tag4` VARCHAR(100), IN `p_tag5` VARCHAR(100), IN `p_sortBy` VARCHAR(20), IN `p_page` INT)
BEGIN
    DECLARE v_offset INT DEFAULT 0;
    
    -- Calculate offset for pagination
    SET v_offset = (p_page - 1) * 20;
    
    -- Main query with all conditions using named parameters
    SELECT 
        b.buildID, 
        b.buildName, 
        COALESCE(c.className, 'No Class') as className,
        b.userID, 
        i.imageID,
        COALESCE(i.filePath, 'no-image.jpg') as filePath, 
        u.userName, 
        COALESCE(g.gameName, 'Unknown Game') as gameName, 
        GROUP_CONCAT(DISTINCT t.tagName SEPARATOR ', ') as tags
    FROM Builds b 
    LEFT JOIN Images i ON i.buildID = b.buildID AND i.typeID = 1
    LEFT JOIN Users u ON u.userID = b.userID 
    LEFT JOIN Classes c ON c.classID = b.classID
    LEFT JOIN Games g ON g.gameID = b.gameID
    LEFT JOIN BuildTagLinks btl ON btl.buildID = b.buildID
    LEFT JOIN Tags t ON t.tagID = btl.tagID
    WHERE b.isPublic = 1 
    
    -- Game filter (always required)
    AND b.gameID = p_gameID
    
    -- Build name filter (optional)
    AND (p_buildName IS NULL OR p_buildName = '' OR LOWER(b.buildName) LIKE LOWER(CONCAT('%', p_buildName, '%')))
    
    -- Author filter (optional)  
    AND (p_author IS NULL OR p_author = '' OR LOWER(u.userName) LIKE LOWER(CONCAT('%', p_author, '%')))
    
    -- Class filter (optional, 0 means any class)
    AND (p_classID IS NULL OR p_classID = 0 OR b.classID = p_classID)
    
    -- Tag filters (all must match if provided) - each tag is optional
    AND (p_tag1 IS NULL OR p_tag1 = '' OR EXISTS (
        SELECT 1 FROM BuildTagLinks btl1 
        JOIN Tags t1 ON t1.tagID = btl1.tagID 
        WHERE btl1.buildID = b.buildID AND LOWER(t1.tagName) = LOWER(p_tag1)
    ))
    
    AND (p_tag2 IS NULL OR p_tag2 = '' OR EXISTS (
        SELECT 1 FROM BuildTagLinks btl2 
        JOIN Tags t2 ON t2.tagID = btl2.tagID 
        WHERE btl2.buildID = b.buildID AND LOWER(t2.tagName) = LOWER(p_tag2)
    ))
    
    AND (p_tag3 IS NULL OR p_tag3 = '' OR EXISTS (
        SELECT 1 FROM BuildTagLinks btl3 
        JOIN Tags t3 ON t3.tagID = btl3.tagID 
        WHERE btl3.buildID = b.buildID AND LOWER(t3.tagName) = LOWER(p_tag3)
    ))
    
    AND (p_tag4 IS NULL OR p_tag4 = '' OR EXISTS (
        SELECT 1 FROM BuildTagLinks btl4 
        JOIN Tags t4 ON t4.tagID = btl4.tagID 
        WHERE btl4.buildID = b.buildID AND LOWER(t4.tagName) = LOWER(p_tag4)
    ))
    
    AND (p_tag5 IS NULL OR p_tag5 = '' OR EXISTS (
        SELECT 1 FROM BuildTagLinks btl5 
        JOIN Tags t5 ON t5.tagID = btl5.tagID 
        WHERE btl5.buildID = b.buildID AND LOWER(t5.tagName) = LOWER(p_tag5)
    ))
    
    -- Group by to handle tag concatenation
    GROUP BY b.buildID, b.buildName, c.className, b.userID, i.imageID, i.filePath, u.userName, g.gameName
    
    -- Sorting
    ORDER BY 
        CASE WHEN p_sortBy = 'oldest' THEN b.buildID END ASC,
        CASE WHEN p_sortBy = 'newest' OR p_sortBy IS NULL THEN b.buildID END DESC
        
    -- Pagination
    LIMIT 20 OFFSET v_offset;
    
END$$
DELIMITER ;