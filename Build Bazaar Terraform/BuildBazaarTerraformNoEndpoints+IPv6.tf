# You need to setup the following for the program to fully function
# SES
# Domain + SSL Cert
#


variable "environment" {
  description = "Environment Name (dev, prod)"
  type        = string
  default     = "dev"
}
variable "vpc_cidr_block" {
  description = "The CIDR block for the VPC"
  type        = string
}

variable "subnet_cidr_blocks" {
  description = "A list of CIDR blocks for subnets"
  type        = list(string)
}

variable "docker_image" {
  description = "The docker image to be used for the EC2 Instance"
  type        = string
}

variable "db_username" {
  description = "Username for the MYSQL database account"
  type        = string
}

variable "db_password" {
  description = "Password for the MYSQL database account"
  type        = string
}

variable "aws_region" {
  description = "Region to use for template (currently only works for us-east-1"
  type        = string
}

variable "certificate_arn" {
  description = "SSL Certificate ARN"
  type        = string
}

variable "domain_name" {
  description = "Domain name used for ALB"
  type        = string
}

variable "r53_hosted_zone_id" {
  description = "Route 53 hosted zone id used for ALB"
  type        = string
}

locals {
  commonTags = {
    Project     = "BuildBazaar"
    Environment = var.environment
  }
}

provider "aws" {
  region = var.aws_region
}

resource "aws_vpc" "buildbazaar_vpc" {
  cidr_block                       = var.vpc_cidr_block
  instance_tenancy                 = "default"
  enable_dns_support               = true #temp
  enable_dns_hostnames             = true #temp
  assign_generated_ipv6_cidr_block = true
  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-vpc-${var.environment}" })
  )
}

resource "aws_subnet" "buildbazaar_subnet_public1" {
  vpc_id                          = aws_vpc.buildbazaar_vpc.id
  cidr_block                      = var.subnet_cidr_blocks[0]
  ipv6_cidr_block                 = cidrsubnet(aws_vpc.buildbazaar_vpc.ipv6_cidr_block, 8, 1)
  availability_zone               = "us-east-1a"
  map_public_ip_on_launch         = false
  assign_ipv6_address_on_creation = true
  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-subnet-public1-${var.environment}" })
  )
}

resource "aws_subnet" "buildbazaar_subnet_public2" {
  vpc_id                          = aws_vpc.buildbazaar_vpc.id
  cidr_block                      = var.subnet_cidr_blocks[1]
  ipv6_cidr_block                 = cidrsubnet(aws_vpc.buildbazaar_vpc.ipv6_cidr_block, 8, 2)
  availability_zone               = "us-east-1b"
  map_public_ip_on_launch         = false
  assign_ipv6_address_on_creation = true
  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-subnet-public2-${var.environment}" })
  )
}

resource "aws_subnet" "buildbazaar_subnet_private1" {
  vpc_id                          = aws_vpc.buildbazaar_vpc.id
  cidr_block                      = var.subnet_cidr_blocks[2]
  ipv6_cidr_block                 = cidrsubnet(aws_vpc.buildbazaar_vpc.ipv6_cidr_block, 8, 3)
  availability_zone               = "us-east-1a"
  map_public_ip_on_launch         = false
  assign_ipv6_address_on_creation = true
  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-subnet-private1-${var.environment}" })
  )
}

resource "aws_subnet" "buildbazaar_subnet_private2" {
  vpc_id                          = aws_vpc.buildbazaar_vpc.id
  cidr_block                      = var.subnet_cidr_blocks[3]
  ipv6_cidr_block                 = cidrsubnet(aws_vpc.buildbazaar_vpc.ipv6_cidr_block, 8, 4)
  availability_zone               = "us-east-1b"
  map_public_ip_on_launch         = false
  assign_ipv6_address_on_creation = true
  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-subnet-private2-${var.environment}" })
  )
}

resource "aws_internet_gateway" "buildbazaar_igw" {
  vpc_id = aws_vpc.buildbazaar_vpc.id
  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-igw-${var.environment}" })
  )
}

resource "aws_egress_only_internet_gateway" "buildbazaar_eigw" {
  vpc_id = aws_vpc.buildbazaar_vpc.id
  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-eigw-${var.environment}" })
  )
}
resource "aws_eip" "ipv6_eip" {
  domain               = "vpc"
  network_border_group = "us-east-1"
}

resource "aws_vpc_endpoint" "buildbazaar_vpc_endpoint_s3" {
  vpc_id            = aws_vpc.buildbazaar_vpc.id
  service_name      = "com.amazonaws.us-east-1.s3"
  vpc_endpoint_type = "Gateway"
  route_table_ids = [
    aws_route_table.buildbazaar_rtb_private.id,
  ]
  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-vpc-endpoint-s3-${var.environment}" })
  )
}

resource "aws_route_table" "buildbazaar_rtb_private" {
  vpc_id = aws_vpc.buildbazaar_vpc.id
  route {
    cidr_block = "0.0.0.0/0"
    #nat_gateway_id = aws_nat_gateway.buildbazaar_nat_gateway.id
    network_interface_id = aws_instance.nat_instance.primary_network_interface_id
  }
  route {
    ipv6_cidr_block        = "::/0"
    egress_only_gateway_id = aws_egress_only_internet_gateway.buildbazaar_eigw.id
  }
  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-rtb-private-${var.environment}" })
  )
}

resource "aws_route_table" "buildbazaar_rtb_public" {
  vpc_id = aws_vpc.buildbazaar_vpc.id
  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.buildbazaar_igw.id
  }
  route {
    ipv6_cidr_block = "::/0"
    gateway_id      = aws_internet_gateway.buildbazaar_igw.id
  }
  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-rtb-public-${var.environment}" })
  )
}

resource "aws_route_table_association" "rta_private1" {
  route_table_id = aws_route_table.buildbazaar_rtb_private.id
  subnet_id      = aws_subnet.buildbazaar_subnet_private1.id
}

resource "aws_route_table_association" "rta_private2" {
  route_table_id = aws_route_table.buildbazaar_rtb_private.id
  subnet_id      = aws_subnet.buildbazaar_subnet_private2.id
}

resource "aws_route_table_association" "rta_public1" {
  route_table_id = aws_route_table.buildbazaar_rtb_public.id
  subnet_id      = aws_subnet.buildbazaar_subnet_public1.id
}

resource "aws_route_table_association" "rta_public2" {
  route_table_id = aws_route_table.buildbazaar_rtb_public.id
  subnet_id      = aws_subnet.buildbazaar_subnet_public2.id
}

# Security Group allowing inbound on port 80 for the ALB and instances
resource "aws_security_group" "buildbazaar_public_sg" {
  vpc_id = aws_vpc.buildbazaar_vpc.id
  name   = "BuildBazaar-public-sg-${var.environment}"

  ingress {
    from_port        = 80
    to_port          = 80
    protocol         = "tcp"
    cidr_blocks      = ["0.0.0.0/0"]
    ipv6_cidr_blocks = ["::/0"]
  }

  ingress {
    from_port        = 443
    to_port          = 443
    protocol         = "tcp"
    cidr_blocks      = ["0.0.0.0/0"]
    ipv6_cidr_blocks = ["::/0"]
  }

  # Allow all outbound traffic from instances
  egress {
    from_port        = 0
    to_port          = 0
    protocol         = "-1"
    cidr_blocks      = ["0.0.0.0/0"]
    ipv6_cidr_blocks = ["::/0"]
  }

  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-public-sg-${var.environment}" })
  )
}

# Security Group for EC2 Instances (only allow traffic from the ALB)
resource "aws_security_group" "buildbazaar_rds_sg" {
  vpc_id = aws_vpc.buildbazaar_vpc.id
  name   = "BuildBazaar-RDS-sg-${var.environment}"

  # Only allow inbound traffic from the ALB's security group
  ingress {
    from_port = 3306
    to_port   = 3306
    protocol  = "tcp"
    security_groups = [
      aws_security_group.buildbazaar_private_sg.id,
      aws_security_group.rds_ec2_sg.id
    ]
  }

  # Allow all outbound traffic
  egress {
    from_port        = 0
    to_port          = 0
    protocol         = "-1"
    cidr_blocks      = ["0.0.0.0/0"]
    ipv6_cidr_blocks = ["::/0"]
  }

  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-rds-sg-${var.environment}" })
  )
}

# Security Group for EC2 Instances (only allow traffic from the ALB)
resource "aws_security_group" "buildbazaar_private_sg" {
  vpc_id = aws_vpc.buildbazaar_vpc.id
  name   = "BuildBazaar-private-sg-${var.environment}"

  # Only allow inbound traffic from the ALB's security group
  ingress {
    from_port       = 80
    to_port         = 80
    protocol        = "tcp"
    security_groups = [aws_security_group.buildbazaar_public_sg.id]
  }

  ingress {
    from_port       = 443
    to_port         = 443
    protocol        = "tcp"
    security_groups = [aws_security_group.buildbazaar_ssm_sg.id]
  }

  # Allow all outbound traffic
  egress {
    from_port        = 0
    to_port          = 0
    protocol         = "-1"
    cidr_blocks      = ["0.0.0.0/0"]
    ipv6_cidr_blocks = ["::/0"]
  }

  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-private-sg-${var.environment}" })
  )
}

resource "aws_security_group" "buildbazaar_ssm_sg" {
  vpc_id = aws_vpc.buildbazaar_vpc.id
  name   = "BuildBazaar-ssm-sg-${var.environment}"

  ingress {
    from_port        = 443
    to_port          = 443
    protocol         = "tcp"
    cidr_blocks      = [aws_vpc.buildbazaar_vpc.cidr_block]
    ipv6_cidr_blocks = [aws_vpc.buildbazaar_vpc.ipv6_cidr_block]
  }

  egress {
    from_port        = 0
    to_port          = 0
    protocol         = "-1"
    cidr_blocks      = ["0.0.0.0/0"]
    ipv6_cidr_blocks = ["::/0"]
  }
  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-ssm-sg-${var.environment}" })
  )
}

resource "aws_security_group" "rds_ec2_sg" {
  vpc_id = aws_vpc.buildbazaar_vpc.id
  name   = "BuildBazaar-rds-ec2-sg-${var.environment}"

  #   ingress {
  #     from_port   = 22
  #     to_port     = 22
  #     protocol    = "tcp"
  #     cidr_blocks = ["0.0.0.0/0"]  # Allow SSH from anywhere, adjust for security
  #     ipv6_cidr_blocks = ["::/0"]  # Allow SSH from anywhere, adjust for security
  #   }


  egress {
    from_port        = 0
    to_port          = 0
    protocol         = "-1"
    cidr_blocks      = ["0.0.0.0/0"]
    ipv6_cidr_blocks = ["::/0"]
  }

  tags = merge(
    local.commonTags,
    tomap({ "Name" = "rds-ec2-sg-${var.environment}" })
  )
}

resource "aws_security_group" "nat_sg" {
  name   = "BuildBazaar-nat-sg-${var.environment}"
  vpc_id = aws_vpc.buildbazaar_vpc.id

  ingress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = [aws_subnet.buildbazaar_subnet_private1.cidr_block, aws_subnet.buildbazaar_subnet_private2.cidr_block]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-nat-sg-${var.environment}" })
  )
}

# Replaced with NAT Instance due to cost
# resource "aws_eip" "buildbazaar_nat_gateway_ip" {
#   domain = "vpc"
# }
#
# resource "aws_nat_gateway" "buildbazaar_nat_gateway" {
#   allocation_id = aws_eip.buildbazaar_nat_gateway_ip.id
#   subnet_id = aws_subnet.buildbazaar_subnet_public1.id
# }

# NAT Instance
resource "aws_instance" "nat_instance" {
  ami                         = "ami-01bdb77dda67ff6b7"
  instance_type               = "t2.micro"
  subnet_id                   = aws_subnet.buildbazaar_subnet_public1.id
  vpc_security_group_ids      = [aws_security_group.nat_sg.id]
  associate_public_ip_address = true
  ipv6_address_count          = 1

  # Disable source/destination check so that the instance can forward traffic
  source_dest_check = false

  tags = merge(
    local.commonTags,
    tomap({ "Name" = "NAT-Instance-${var.environment}" })
  )
}

##### THESE ARE VERY EXPENSIVE, COMMENT WHEN NOT IN USE ###########
# resource "aws_vpc_endpoint" "buildbazaar_ssm_endpoint" {
#   vpc_id            = aws_vpc.buildbazaar_vpc.id
#   service_name      = "com.amazonaws.${var.aws_region}.ssm"
#   vpc_endpoint_type = "Interface"
#
#   security_group_ids = [
#     aws_security_group.buildbazaar_ssm_sg.id
#   ]
#   subnet_ids = [
#     aws_subnet.buildbazaar_subnet_private1.id,
#     aws_subnet.buildbazaar_subnet_private2.id
#   ]
#
#   private_dns_enabled = true
#
#   tags = merge(
#     local.commonTags,
#     tomap({ "Name" = "buildbazaar_ssm_endpoint-${var.environment}" })
#   )
# }
#
# resource "aws_vpc_endpoint" "ec2messages_endpoint" {
#   vpc_id            = aws_vpc.buildbazaar_vpc.id
#   service_name      = "com.amazonaws.${var.aws_region}.ec2messages"
#   vpc_endpoint_type = "Interface"
#
#   security_group_ids = [
#     aws_security_group.buildbazaar_ssm_sg.id
#   ]
#
#   subnet_ids = [
#     aws_subnet.buildbazaar_subnet_private1.id,
#     aws_subnet.buildbazaar_subnet_private2.id
#   ]
#
#   private_dns_enabled = true
#
#   tags = merge(
#     local.commonTags,
#     tomap({ "Name" = "ec2messages_endpoint-${var.environment}" })
#   )
# }
#
# resource "aws_vpc_endpoint" "ssmmessages_endpoint" {
#   vpc_id            = aws_vpc.buildbazaar_vpc.id
#   service_name      = "com.amazonaws.${var.aws_region}.ssmmessages"
#   vpc_endpoint_type = "Interface"
#
#   security_group_ids = [
#     aws_security_group.buildbazaar_ssm_sg.id
#   ]
#
#   subnet_ids = [
#     aws_subnet.buildbazaar_subnet_private1.id,
#     aws_subnet.buildbazaar_subnet_private2.id
#   ]
#
#   private_dns_enabled = true
#
#   tags = merge(
#     local.commonTags,
#     tomap({ "Name" = "ec2messages_endpoint-${var.environment}" })
#   )
# }
############ END EXPENSIVE ZONE ######################



# Application Load Balancer
resource "aws_lb" "buildbazaar_alb" {
  name               = "BuildBazaar-alb-${var.environment}"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.buildbazaar_public_sg.id]
  subnets = [
    aws_subnet.buildbazaar_subnet_public1.id,
    aws_subnet.buildbazaar_subnet_public2.id
  ]

  ip_address_type = "dualstack"

  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-lb-${var.environment}" })
  )
}

# Target Group for EC2 instances
resource "aws_lb_target_group" "buildbazaar_tg" {
  name     = "BuildBazaar-tg-${var.environment}"
  port     = 80
  protocol = "HTTP"
  vpc_id   = aws_vpc.buildbazaar_vpc.id

  health_check {
    path                = "/"
    protocol            = "HTTP"
    port                = "traffic-port"
    healthy_threshold   = 2
    unhealthy_threshold = 2
    timeout             = 5
    interval            = 30
  }

  tags = merge(
    local.commonTags,
    tomap({ "Name" = "BuildBazaar-tg-${var.environment}" })
  )
}


resource "aws_autoscaling_group" "buildbazaar_asg" {
  desired_capacity = 1  # Adjust as needed
  min_size         = 1  # Minimum instances to run
  max_size         = 10 # Maximum instances to scale
  vpc_zone_identifier = [
    aws_subnet.buildbazaar_subnet_private1.id,
    aws_subnet.buildbazaar_subnet_private2.id
  ]

  launch_template {
    id      = aws_launch_template.buildbazaar_launch_template.id
    version = "$Latest"
  }

  target_group_arns = [aws_lb_target_group.buildbazaar_tg.arn]

  # health_check_type         = "ELB"
  health_check_type         = "EC2" #Changed to EC2 only checks due to SSM IPv4 Issue
  health_check_grace_period = 600   # Time to allow instances to boot up

  tag {
    key                 = "Name"
    value               = "BuildBazaar-asg-instance-${var.environment}"
    propagate_at_launch = true
  }
}


# ALB Listener for HTTP (port 80)
resource "aws_lb_listener" "https_listener" {
  load_balancer_arn = aws_lb.buildbazaar_alb.arn
  port              = "443"
  protocol          = "HTTPS"
  ssl_policy        = "ELBSecurityPolicy-2016-08"
  certificate_arn   = var.certificate_arn

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.buildbazaar_tg.arn
  }
}

resource "aws_route53_record" "buildbazaar_route53_record_1" {
  name    = var.domain_name
  type    = "A"
  zone_id = var.r53_hosted_zone_id

  alias {
    evaluate_target_health = true
    name                   = aws_lb.buildbazaar_alb.dns_name
    zone_id                = aws_lb.buildbazaar_alb.zone_id
  }
}

resource "aws_route53_record" "cdn_record" {
  name    = "cdn.${var.domain_name}"
  type    = "A"
  zone_id = var.r53_hosted_zone_id

  alias {
    evaluate_target_health = false
    name                   = aws_cloudfront_distribution.s3_distribution.domain_name
    zone_id                = aws_cloudfront_distribution.s3_distribution.hosted_zone_id
  }
}

resource "aws_route53_record" "buildbazaar_route53_record2" {
  name    = "www.${var.domain_name}"
  type    = "A"
  zone_id = var.r53_hosted_zone_id

  alias {
    evaluate_target_health = true
    name                   = aws_lb.buildbazaar_alb.dns_name
    zone_id                = aws_lb.buildbazaar_alb.zone_id
  }
}

resource "aws_autoscaling_policy" "scale_up" {
  name                   = "scale_up_policy-${var.environment}"
  scaling_adjustment     = 1
  adjustment_type        = "ChangeInCapacity"
  cooldown               = 300
  autoscaling_group_name = aws_autoscaling_group.buildbazaar_asg.name
}

resource "aws_autoscaling_policy" "scale_down" {
  name                   = "scale_down_policy-${var.environment}"
  scaling_adjustment     = -1
  adjustment_type        = "ChangeInCapacity"
  cooldown               = 300
  autoscaling_group_name = aws_autoscaling_group.buildbazaar_asg.name
}

resource "aws_cloudwatch_metric_alarm" "scale_up_alarm" {
  alarm_name          = "buildbazaar-scale-up-alarm-${var.environment}"
  alarm_description   = "Triggers scale up when average CPU > 70% over 2 minutes"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "CPUUtilization"
  namespace           = "AWS/EC2"
  period              = 60 # Check every 60 seconds
  statistic           = "Average"
  threshold           = 70

  dimensions = {
    AutoScalingGroupName = aws_autoscaling_group.buildbazaar_asg.name
  }

  alarm_actions = [
    aws_autoscaling_policy.scale_up.arn
  ]
}

resource "aws_cloudwatch_metric_alarm" "scale_down_alarm" {
  alarm_name          = "buildbazaar-scale-down-alarm-${var.environment}"
  alarm_description   = "Triggers scale down when average CPU < 20% over 2 minutes"
  comparison_operator = "LessThanThreshold"
  evaluation_periods  = 2
  metric_name         = "CPUUtilization"
  namespace           = "AWS/EC2"
  period              = 60 # Check every 60 seconds
  statistic           = "Average"
  threshold           = 20

  dimensions = {
    AutoScalingGroupName = aws_autoscaling_group.buildbazaar_asg.name
  }

  alarm_actions = [
    aws_autoscaling_policy.scale_down.arn
  ]
}



resource "aws_launch_template" "buildbazaar_launch_template" {
  name_prefix   = "BuildBazaar-launch-template-${var.environment}"
  image_id      = "ami-0182f373e66f89c85"
  instance_type = "t2.micro"

  network_interfaces {
    associate_public_ip_address = false
    ipv6_address_count          = 1
    security_groups             = [aws_security_group.buildbazaar_private_sg.id, aws_security_group.rds_ec2_sg.id]
  }

  iam_instance_profile {
    name = aws_iam_instance_profile.ec2_instance_profile.name
  }

  key_name = aws_key_pair.buildbazaar_rds_keypair.key_name # Optional if you need SSH access

  #vpc_security_group_ids = [aws_security_group.buildbazaar_private_sg.id]

  user_data = base64encode(<<-EOF
              #!/bin/bash
              yum update -y
              yum install -y docker
              service docker start
              usermod -a -G docker ec2-user
              docker run -d -p 80:8080 -e ENVIRONMENT="${var.environment}" -e AWS_REGION="${var.aws_region}" ${var.docker_image}
              EOF
  )

  tag_specifications {
    resource_type = "instance"
    tags = merge(
      local.commonTags,
      tomap({ "Name" = "BuildBazaar-instance-${var.environment}" })
    )
  }
}

resource "aws_iam_role" "ec2_role" {
  name = "BuildBazaar-ec2-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ec2.amazonaws.com"
        }
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "ec2_s3_policy" {
  role       = aws_iam_role.ec2_role.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonS3FullAccess"
}

resource "aws_iam_role_policy_attachment" "ec2_ssm_read_policy" {
  role       = aws_iam_role.ec2_role.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMReadOnlyAccess"
}

resource "aws_iam_role_policy_attachment" "ec2_ssm_policy" {
  role       = aws_iam_role.ec2_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonEC2RoleforSSM"
}

resource "aws_iam_role_policy_attachment" "ec2_ssm_core_policy" {
  role       = aws_iam_role.ec2_role.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

resource "aws_iam_role_policy_attachment" "ec2_cf_policy" {
  role       = aws_iam_role.ec2_role.name
  policy_arn = "arn:aws:iam::aws:policy/CloudFrontReadOnlyAccess"
}

resource "aws_iam_role_policy" "ec2_ses_policy" {
  name = "EC2SESSendPermissions"
  role = aws_iam_role.ec2_role.name

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "ses:SendEmail",
          "ses:SendRawEmail"
        ]
        Resource = [
          "arn:aws:ses:${var.aws_region}:*"
        ]
      }
    ]
  })
}

resource "aws_iam_role_policy" "ec2_ssm_session_policy" {
  name = "EC2SSMSessionPermissions"
  role = aws_iam_role.ec2_role.name

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "ssm:StartSession",
          "ssm:TerminateSession",
          "ssm:ResumeSession",
          "ssm:DescribeSessions",
          "ssm:GetConnectionStatus"
        ]
        Resource = "*"
      }
    ]
  })
}

# IAM Instance Profile for EC2
resource "aws_iam_instance_profile" "ec2_instance_profile" {
  name = "BuildBazaar-ec2-instance-profile"
  role = aws_iam_role.ec2_role.name
}

#Generate secret key for user token validation
resource "random_password" "secret_key" {
  length           = 32 # Adjust length as needed
  special          = false
  override_special = "_%" # Optional: Customize special characters
}

# Generate a key pair for RDS
resource "tls_private_key" "buildbazaar_rds_keypair" {
  algorithm = "RSA"
  rsa_bits  = 2048
}

# Save the private key locally
resource "local_file" "rds_private_key" {
  filename        = "${path.module}/Keys/buildbazaar_rds_keypair-${var.environment}.pem"
  content         = tls_private_key.buildbazaar_rds_keypair.private_key_pem
  file_permission = "0600"
}

# Import the public key into AWS as a key pair
resource "aws_key_pair" "buildbazaar_rds_keypair" {
  key_name   = "buildbazaar_rds_keypair-${var.environment}" # Key pair name to use in the instance
  public_key = tls_private_key.buildbazaar_rds_keypair.public_key_openssh
}

# EC2 Instance with MySQL Client
resource "aws_instance" "public_ec2_db_access" {
  ami                         = "ami-0182f373e66f89c85" # Amazon Linux 2 AMI
  instance_type               = "t2.micro"
  key_name                    = aws_key_pair.buildbazaar_rds_keypair.key_name # Replace with your key pair name
  subnet_id                   = aws_subnet.buildbazaar_subnet_public1.id      # Public Subnet
  vpc_security_group_ids      = [aws_security_group.rds_ec2_sg.id, aws_security_group.buildbazaar_public_sg.id]
  associate_public_ip_address = false
  ipv6_address_count          = 1
  iam_instance_profile        = aws_iam_instance_profile.ec2_instance_profile.name

  user_data = <<-EOF
              #!/bin/bash
              sudo yum update -y
              sudo wget https://dev.mysql.com/get/mysql80-community-release-el9-1.noarch.rpm
              sudo dnf install mysql80-community-release-el9-1.noarch.rpm -y
              sudo rpm --import https://repo.mysql.com/RPM-GPG-KEY-mysql-2023
              sudo dnf install mysql-community-client -y
              EOF


  tags = merge(
    local.commonTags,
    tomap({ "Name" = "Public-EC2-SQL-Access-${var.environment}" })
  )
}

#Variables for server code
# Sensitive Parameters
resource "aws_ssm_parameter" "db_username" {
  name  = "DB_USERNAME_${var.environment}"
  type  = "SecureString"
  value = var.db_username # Replace with the actual DB username
}

resource "aws_ssm_parameter" "db_password" {
  name  = "DB_PASSWORD_${var.environment}"
  type  = "SecureString"
  value = var.db_password # Replace with the actual DB password
}

resource "aws_ssm_parameter" "secret_key" {
  name  = "SECRET_KEY_${var.environment}"
  type  = "SecureString"
  value = random_password.secret_key.result # Use random_password or equivalent
}

resource "aws_ssm_parameter" "cloudfront_private_key" {
  name  = "CLOUDFRONT_PRIVATE_KEY_${var.environment}"
  type  = "SecureString"
  value = tls_private_key.buildbazaar_cloudfront_keypair.private_key_pem
}

resource "aws_ssm_parameter" "cloudfront_key_pair_id" {
  name  = "CLOUDFRONT_KEY_PAIR_ID_${var.environment}"
  type  = "SecureString"
  value = aws_cloudfront_key_group.buildbazaar_cloudfront_key_group.id
}

resource "aws_ssm_parameter" "cloudfront_distribution_domain" {
  name = "CLOUDFRONT_DISTRIBUTION_DOMAIN_${var.environment}"
  type = "SecureString"
  #value = aws_cloudfront_distribution.s3_distribution.domain_name
  value = "cdn.${var.domain_name}" #custom cloudfront urls
}

# Non-Sensitive Parameters
resource "aws_ssm_parameter" "aws_region" {
  name  = "REGION_${var.environment}"
  type  = "String"
  value = var.aws_region
}

resource "aws_ssm_parameter" "db_host" {
  name  = "DB_HOST_${var.environment}"
  type  = "String"
  value = split(":", aws_db_instance.buildbazaar_mysql.endpoint)[0] # Extract the host without the port
}

resource "aws_ssm_parameter" "db_port" {
  name  = "DB_PORT_${var.environment}"
  type  = "String"
  value = "3306" # Default MySQL port
}

resource "aws_ssm_parameter" "db_name" {
  name  = "DB_NAME_${var.environment}"
  type  = "String"
  value = aws_db_instance.buildbazaar_mysql.db_name # Replace with your actual database name
}

resource "aws_ssm_parameter" "jwt_issuer" {
  name  = "JWT_ISSUER_${var.environment}"
  type  = "String"
  value = var.domain_name
}

resource "aws_ssm_parameter" "jwt_audience" {
  name  = "JWT_AUDIENCE_${var.environment}"
  type  = "String"
  value = var.domain_name
}

resource "aws_ssm_parameter" "bucket_name" {
  name  = "BUCKET_NAME_${var.environment}"
  type  = "String"
  value = aws_s3_bucket.buildbazaar_bucket.bucket # Replace with your actual S3 bucket name
}

# Modify RDS Security Group to Allow Access from EC2 Instance
# resource "aws_security_group_rule" "allow_ec2_to_rds" {
#   type              = "ingress"
#   from_port         = 3306
#   to_port           = 3306
#   protocol          = "tcp"
#   security_group_id = aws_security_group.buildbazaar_rds_sg.id  # RDS SG
#   source_security_group_id = aws_security_group.rds_ec2_sg.id   # EC2 SG
# }

resource "aws_db_subnet_group" "buildbazaar_rds_subnet_group" {
  name       = "buildbazaar-rds-subnet-group-${var.environment}"
  subnet_ids = [aws_subnet.buildbazaar_subnet_private1.id, aws_subnet.buildbazaar_subnet_private2.id] #temp change back to private

  tags = merge(
    local.commonTags,
    tomap({ "Name" = "buildbazaar-rds-subnet-group-${var.environment}" })
  )
}

resource "aws_db_instance" "buildbazaar_mysql" {
  identifier             = "buildbazaar-db-${var.environment}"
  allocated_storage      = 20 # Minimum required for free-tier
  storage_type           = "gp2"
  engine                 = "mysql"
  engine_version         = "8.0"
  instance_class         = "db.t3.micro" # Free-tier eligible
  username               = var.db_username
  password               = var.db_password
  db_subnet_group_name   = aws_db_subnet_group.buildbazaar_rds_subnet_group.name
  vpc_security_group_ids = [aws_security_group.buildbazaar_rds_sg.id]
  publicly_accessible    = false
  skip_final_snapshot    = true
  apply_immediately      = true
  db_name                = "BuildBazaarDB"

  tags = merge(
    local.commonTags,
    tomap({ "Name" = "buildbazaar-db-${var.environment}" })
  )
}

# Create S3 Bucket
resource "aws_s3_bucket" "buildbazaar_bucket" {
  bucket = "buildbazaar-bucket-${var.environment}"

  tags = merge(
    local.commonTags,
    tomap({ "Name" = "buildbazaar-production-bucket-${var.environment}" })
  )

}

# S3 Bucket Policy for CloudFront Access
resource "aws_s3_bucket_policy" "buildbazaar_bucket_policy" {
  bucket = aws_s3_bucket.buildbazaar_bucket.id

  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Sid    = "AllowCloudFrontAccess",
        Effect = "Allow",
        Principal = {
          Service = "cloudfront.amazonaws.com"
        },
        Action   = "s3:GetObject",
        Resource = "${aws_s3_bucket.buildbazaar_bucket.arn}/*"
        Condition = {
          StringEquals = {
            "AWS:SourceArn" = aws_cloudfront_distribution.s3_distribution.arn
          }
        }
      }
    ]
  })
}

resource "aws_s3_bucket_cors_configuration" "buildbazaar_bucket_cors" {
  bucket = aws_s3_bucket.buildbazaar_bucket.id

  cors_rule {
    allowed_headers = ["*"]
    allowed_methods = ["GET"]
    allowed_origins = [
      "https://${aws_cloudfront_distribution.s3_distribution.domain_name}", # CloudFront domain
      "https://www.${var.domain_name}",
      "https://${var.domain_name}",
      "https://cdn.${var.domain_name}",
      "http://www.${var.domain_name}",
      "http://${var.domain_name}",
      "http://cdn.${var.domain_name}"
    ]
    expose_headers  = ["ETag"]
    max_age_seconds = 3600
  }
}

# Generate a key pair for CloudFront
resource "tls_private_key" "buildbazaar_cloudfront_keypair" {
  algorithm = "RSA"
  rsa_bits  = 2048
}

resource "aws_cloudfront_public_key" "cloudfront_public_key" {
  name        = "BuildBazaar-Cloudfront-Public-Key-${var.environment}"
  comment     = "Key for accessing files in the BuildBazaar Bucket via CloudFront"
  encoded_key = tls_private_key.buildbazaar_cloudfront_keypair.public_key_pem
}

resource "aws_cloudfront_key_group" "buildbazaar_cloudfront_key_group" {
  name  = "buildbazaar-cloudfront-key-group-${var.environment}"
  items = [aws_cloudfront_public_key.cloudfront_public_key.id]
}

resource "aws_cloudfront_origin_access_control" "buildbazaar_oac" {
  name                              = "BuildBazaarOAC-${var.environment}"
  description                       = "OAC for accessing S3 bucket securely"
  origin_access_control_origin_type = "s3"

  signing_behavior = "always"
  signing_protocol = "sigv4"
}

# CloudFront Distribution
resource "aws_cloudfront_distribution" "s3_distribution" {
  origin {
    domain_name              = aws_s3_bucket.buildbazaar_bucket.bucket_regional_domain_name
    origin_id                = "S3-${aws_s3_bucket.buildbazaar_bucket.id}"
    origin_access_control_id = aws_cloudfront_origin_access_control.buildbazaar_oac.id

  }

  enabled = true

  default_cache_behavior {
    target_origin_id       = "S3-${aws_s3_bucket.buildbazaar_bucket.id}"
    viewer_protocol_policy = "redirect-to-https"

    allowed_methods = ["GET", "HEAD"]
    cached_methods  = ["GET", "HEAD"]

    forwarded_values {
      query_string = false
      headers      = ["Origin"]
      cookies {
        forward = "none"
      }
    }

    default_ttl = 86400
    max_ttl     = 31536000
    min_ttl     = 0
  }

  # Cache Behavior for notes.txt (no caching)
  ordered_cache_behavior {
    path_pattern           = "*/notes.txt"
    target_origin_id       = "S3-${aws_s3_bucket.buildbazaar_bucket.id}"
    viewer_protocol_policy = "redirect-to-https"

    allowed_methods = ["GET", "HEAD"]
    cached_methods  = ["GET", "HEAD"]

    forwarded_values {
      query_string = false
      headers      = ["Origin"]
      cookies {
        forward = "none"
      }
    }

    default_ttl = 0
    max_ttl     = 0
    min_ttl     = 0
  }

  # Add your custom domain and SSL certificate
  aliases = ["cdn.${var.domain_name}"]

  viewer_certificate {
    acm_certificate_arn      = var.certificate_arn
    ssl_support_method       = "sni-only"
    minimum_protocol_version = "TLSv1.2_2021"
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  tags = local.commonTags
}

resource "aws_cloudwatch_dashboard" "buildbazaar_dashboard" {
  dashboard_name = "buildbazaar-dashboard-${var.environment}"
  dashboard_body = <<EOF
{
  "widgets": [
    {
      "type": "metric",
      "x": 0,
      "y": 0,
      "width": 6,
      "height": 6,
      "properties": {
        "title": "ALB Request Count",
        "metrics": [
          [ "AWS/ApplicationELB", "RequestCount", "LoadBalancer", "${aws_lb.buildbazaar_alb.arn_suffix}" ]
        ],
        "region": "${var.aws_region}",
        "period": 60,
        "stat": "Sum"
      }
    },
    {
      "type": "metric",
      "x": 6,
      "y": 0,
      "width": 6,
      "height": 6,
      "properties": {
        "title": "ALB HTTP 5XX Errors",
        "metrics": [
          [ "AWS/ApplicationELB", "HTTPCode_Target_5XX_Count", "LoadBalancer", "${aws_lb.buildbazaar_alb.arn_suffix}" ]
        ],
        "region": "${var.aws_region}",
        "period": 60,
        "stat": "Sum"
      }
    },
    {
      "type": "metric",
      "x": 0,
      "y": 6,
      "width": 6,
      "height": 6,
      "properties": {
        "title": "ASG CPU Utilization",
        "metrics": [
          [ "AWS/EC2", "CPUUtilization", "AutoScalingGroupName", "${aws_autoscaling_group.buildbazaar_asg.name}" ]
        ],
        "region": "${var.aws_region}",
        "period": 60,
        "stat": "Average"
      }
    },
    {
      "type": "text",
      "x": 6,
      "y": 6,
      "width": 6,
      "height": 6,
      "properties": {
        "markdown": "# BuildBazaar Dashboard\n\nMonitor traffic and performance for your site."
      }
    }
  ]
}
EOF
}
